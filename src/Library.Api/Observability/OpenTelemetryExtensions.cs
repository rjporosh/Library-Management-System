using Library.Application.Common.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Library.Api.Observability;

/// <summary>
/// OpenTelemetry traces + metrics, exported over OTLP (Jaeger / any collector).
/// Off by default; toggle with FeatureFlags.EnableOpenTelemetry. The exporter
/// is resilient - a missing collector does not stop the app.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddLibraryObservability(
        this IServiceCollection services,
        ObservabilitySettings settings)
    {
        if (!settings.EnableOpenTelemetry)
        {
            return services;
        }

        var resource = ResourceBuilder.CreateDefault()
            .AddService(settings.ServiceName, serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString());

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(settings.ServiceName))
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddSource("Library")
                .AddSource("Npgsql")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(settings.OtlpEndpoint)))
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddMeter("Library")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(settings.OtlpEndpoint)));

        return services;
    }
}
