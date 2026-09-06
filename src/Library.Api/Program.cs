using System.Text.Json.Serialization;
using Library.Api.BackgroundJobs;
using Library.Api.HealthChecks;
using Library.Api.Infrastructure;
using Library.Api.Middleware;
using Library.Api.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Library.Application.Common.Options;
using Library.Application.DependencyInjection;
using Library.Infrastructure.DependencyInjection;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Feature flags / observability settings (appsettings.json "FeatureFlags").
// Bound once here (composition root) and shared as a singleton so every
// layer - middleware, cron job, log writer, log-download endpoint - reads
// the exact same configuration without each depending on IOptions<T>.
var observabilitySettings =
    builder.Configuration.GetSection("FeatureFlags").Get<ObservabilitySettings>()
    ?? new ObservabilitySettings();

var databaseOptions =
    builder.Configuration.GetSection("Database").Get<DatabaseOptions>()
    ?? new DatabaseOptions();

// Observability (OpenTelemetry -> OTLP -> Jaeger). Off unless
// FeatureFlags.EnableOpenTelemetry is true.
builder.Services.AddLibraryObservability(observabilitySettings);

// Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    observabilitySettings,
    databaseOptions,
    builder.Environment.ContentRootPath);

// MVC Controllers - enums serialize as their string name (e.g. "Active",
// not 0) so the UI never has to translate numeric status codes itself,
// and can search/filter by the same names it displays.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

// RFC 7807 problem details for framework-generated errors (bare NotFound(),
// model-binding 400s). Our middleware emits the same shape for thrown/Result
// errors; this keeps the two consistent (adds success/correlationId).
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["success"] = false;
        if (ctx.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id))
        {
            ctx.ProblemDetails.Extensions["correlationId"] = id?.ToString();
        }

        ctx.ProblemDetails.Extensions.TryAdd("errors", Array.Empty<object>());
    };
});

// OpenAPI
builder.Services.AddOpenApi();

// Health checks (Phase 10). Extend with real DB/cache checks as those
// dependencies are introduced (Phase 6/11) - the /health contract below
// does not need to change.
builder.Services.AddHealthChecks()
    .AddCheck<PersistenceHealthCheck>("persistence");

// Midnight member-suspension job (toggle: FeatureFlags.EnableMemberSuspensionCronJob).
builder.Services.AddHostedService<MemberSuspensionCronJob>();

// Localization: English (default) + Bangla, extensible by dropping in a new
// Resources/SharedResources.<culture>.resx (no code change). Culture comes from
// the Accept-Language header, a ?culture= / ?lang= query parameter, or falls
// back to English.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[] { new System.Globalization.CultureInfo("en"), new System.Globalization.CultureInfo("bn") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
    options.SupportedCultures = supported;
    options.SupportedUICultures = supported;
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider
    {
        QueryStringKey = "culture",
        UIQueryStringKey = "lang",
    });
});

// Trust forwarded headers from the reverse proxy / load balancer so
// rate limiting and logging see the real client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Per-client rate limiting (toggle: FeatureFlags.EnableRateLimiting).
builder.Services.AddLibraryRateLimiting(observabilitySettings);

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
app.UseForwardedHeaders();
app.UseRequestLocalization();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (observabilitySettings.EnableRateLimiting)
{
    app.UseRateLimiter();
}

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
var controllers = app.MapControllers();
if (observabilitySettings.EnableRateLimiting)
{
    controllers.RequireRateLimiting(RateLimitingExtensions.PolicyName);
}

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

// Migrate + seed. Any failure here (database unreachable / missing / bad
// credentials) is written to build-error-logs with a diagnosed root cause
// and a fix hint, then rethrown so the host fails loudly.
try
{
    using var scope = app.Services.CreateScope();

    if (databaseOptions.IsRelational)
    {
        var db = scope.ServiceProvider
            .GetRequiredService<Library.Infrastructure.Persistence.LibraryDbContext>();

        if (databaseOptions.MigrateOnStartup)
        {
            await db.Database.MigrateAsync();
        }

        if (databaseOptions.SeedOnStartup)
        {
            await scope.ServiceProvider
                .GetRequiredService<Library.Infrastructure.Persistence.Seed.DatabaseSeeder>()
                .SeedAsync();
        }
    }
    else
    {
        scope.ServiceProvider.GetRequiredService<InMemoryDataSeeder>().Seed();
    }
}
catch (Exception ex)
{
    WriteDatabaseDiagnostic(ex, app.Environment.ContentRootPath, observabilitySettings, databaseOptions);
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

// Startup database failure: classify the cause and write a build-error entry
// that names the provider, host and database and suggests a concrete fix, so a
// developer sees "PostgreSQL is not running / the database does not exist"
// rather than a raw stack trace (spec: "DB down -> clear conscious cause").
static void WriteDatabaseDiagnostic(
    Exception ex,
    string contentRootPath,
    ObservabilitySettings settings,
    DatabaseOptions database)
{
    var (cause, fix) = ex.GetType().Name switch
    {
        "NpgsqlException" or "SocketException" =>
            ($"Cannot reach the '{database.Provider}' database server.",
             "Is the database running? Start it (e.g. `docker compose up -d db`) and check Database:ConnectionString host/port."),
        "PostgresException" when ex.Message.Contains("3D000") =>
            ("The target database does not exist.",
             "Create it, or run `dotnet ef database update` to create the schema."),
        "SqlException" =>
            ($"The '{database.Provider}' database could not be opened.",
             "Verify the server is running and the connection string / credentials are correct."),
        _ when ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) =>
            ("Database authentication failed.",
             "Check the username/password in Database:ConnectionString."),
        _ => ("The database is unavailable or the schema is out of date.",
              "Check the database is reachable and migrations are applied (`dotnet ef database update`)."),
    };

    var host = "unknown";
    var name = "unknown";
    foreach (var part in (database.ConnectionString ?? string.Empty).Split(';'))
    {
        var kv = part.Split('=', 2);
        if (kv.Length != 2) continue;
        var key = kv[0].Trim().ToLowerInvariant();
        if (key is "host" or "server" or "data source") host = kv[1].Trim();
        if (key is "database" or "initial catalog") name = kv[1].Trim();
    }

    if (settings.EnableBuildErrorLogging)
    {
        try
        {
            var dir = Path.IsPathRooted(settings.LogsRootPath)
                ? Path.Combine(settings.LogsRootPath, "build-error-logs")
                : Path.Combine(contentRootPath, settings.LogsRootPath, "build-error-logs");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"build-error-logs-{DateTime.UtcNow:dd-MM-yyyy}.txt");
            File.AppendAllText(file, System.Text.Json.JsonSerializer.Serialize(new
            {
                timestampUtc = DateTime.UtcNow,
                component = "startup/database",
                provider = database.Provider,
                host,
                database = name,
                exceptionType = ex.GetType().FullName,
                message = ex.Message,
                rootCause = cause,
                possibleBestFix = fix,
                stackTrace = ex.StackTrace,
            }) + Environment.NewLine);
        }
        catch
        {
            // never let logging the failure become a second failure
        }
    }

    Console.Error.WriteLine($"[Startup] DATABASE UNAVAILABLE ({database.Provider} @ {host}/{name}): {cause} -> {fix}");
}

public partial class Program;
