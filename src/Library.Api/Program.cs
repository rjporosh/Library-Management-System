using System.Text.Json.Serialization;
using Library.Api.BackgroundJobs;
using Library.Api.HealthChecks;
using Library.Api.Middleware;
using Library.Application.Common.Options;
using Library.Application.DependencyInjection;
using Library.Infrastructure.DependencyInjection;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Feature flags / observability settings (appsettings.json "FeatureFlags").
// Bound once here (composition root) and shared as a singleton so every
// layer - middleware, cron job, log writer, log-download endpoint - reads
// the exact same configuration without each depending on IOptions<T>.
var observabilitySettings =
    builder.Configuration.GetSection("FeatureFlags").Get<ObservabilitySettings>()
    ?? new ObservabilitySettings();

// Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    observabilitySettings,
    builder.Environment.ContentRootPath);

// MVC Controllers - enums serialize as their string name (e.g. "Active",
// not 0) so the UI never has to translate numeric status codes itself,
// and can search/filter by the same names it displays.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

// OpenAPI
builder.Services.AddOpenApi();

// Health checks (Phase 10). Extend with real DB/cache checks as those
// dependencies are introduced (Phase 6/11) - the /health contract below
// does not need to change.
builder.Services.AddHealthChecks()
    .AddCheck<PersistenceHealthCheck>("persistence");

// Midnight member-suspension job (toggle: FeatureFlags.EnableMemberSuspensionCronJob).
builder.Services.AddHostedService<MemberSuspensionCronJob>();

// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

WebApplication app;

try
{
    app = builder.Build();
}
catch (Exception ex)
{
    // The DI container / host failed to build (e.g. bad configuration,
    // a required dependency such as a database or cache is unreachable
    // at startup). The full IAppLogWriter pipeline is not available yet
    // at this point, so write directly to the same build-error-logs file
    // convention as a best-effort fallback.
    WriteBuildErrorFallback(ex, builder.Environment.ContentRootPath, observabilitySettings);
    throw;
}

// Correlation id must run before exception handling so every log entry
// (and every error response) can be tagged with it.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Use the CORS policy
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Library Management System API")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient);
    });
}

// Controllers
app.MapControllers();

// Health check endpoint (toggle: FeatureFlags.EnableHealthCheckEndpoint).
// Returns a simple, structured JSON body so it can be consumed by
// uptime monitors, load balancers, and manual checks alike.
if (observabilitySettings.EnableHealthCheckEndpoint)
{
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    durationMs = e.Value.Duration.TotalMilliseconds
                })
            };

            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(payload));
        }
    });
}

// Seed demo data
try
{
    var seeder = app.Services.GetRequiredService<InMemoryDataSeeder>();
    seeder.Seed();
}
catch (Exception ex)
{
    WriteBuildErrorFallback(ex, app.Environment.ContentRootPath, observabilitySettings);
    throw;
}

try
{
    app.Run();
}
catch (Exception ex)
{
    WriteBuildErrorFallback(ex, app.Environment.ContentRootPath, observabilitySettings);
    throw;
}

static void WriteBuildErrorFallback(
    Exception ex,
    string contentRootPath,
    ObservabilitySettings settings)
{
    if (!settings.EnableBuildErrorLogging)
        return;

    try
    {
        var directory = System.IO.Path.IsPathRooted(settings.LogsRootPath)
            ? System.IO.Path.Combine(settings.LogsRootPath, "build-error-logs")
            : System.IO.Path.Combine(contentRootPath, settings.LogsRootPath, "build-error-logs");

        System.IO.Directory.CreateDirectory(directory);

        var fileName = $"build-error-logs-{DateTime.UtcNow:dd-MM-yyyy}.txt";
        var fullPath = System.IO.Path.Combine(directory, fileName);

        var line = System.Text.Json.JsonSerializer.Serialize(new
        {
            timestampUtc = DateTime.UtcNow,
            exceptionType = ex.GetType().FullName,
            message = ex.Message,
            rootCause = ex.InnerException?.Message ?? ex.Message,
            possibleBestFix =
                "Application failed to start - check that all configured " +
                "dependencies (database, cache, external services) are " +
                "reachable and that appsettings.json is valid for this " +
                "environment.",
            stackTrace = ex.StackTrace
        });

        System.IO.File.AppendAllText(
            fullPath,
            line + Environment.NewLine);
    }
    catch
    {
        // Last-resort fallback: never let logging-the-failure become a
        // second failure. The original exception still propagates via
        // `throw;` at the call site.
        Console.Error.WriteLine(
            $"[Program] Failed to write build-error log for startup failure: {ex}");
    }
}

public partial class Program;
