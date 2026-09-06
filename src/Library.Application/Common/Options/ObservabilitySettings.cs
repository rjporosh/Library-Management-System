namespace Library.Application.Common.Options;

/// <summary>
/// Feature toggles read from the "FeatureFlags" section of
/// appsettings.json. Kept as a plain POCO (no options-pattern
/// package dependency) so it can be bound once at startup in the
/// composition root (Library.Api/Program.cs) and shared as a
/// singleton with both the Api and Infrastructure layers.
/// </summary>
public sealed class ObservabilitySettings
{
    /// <summary>Root folder (relative to content root, or absolute) that all four log streams are written under. Default: "logs".</summary>
    public string LogsRootPath { get; set; } = "logs";

    public bool EnableRuntimeErrorLogging { get; set; } = true;

    public bool EnableBuildErrorLogging { get; set; } = true;

    public bool EnableQueryLogging { get; set; } = true;

    public bool EnableExceptionLogging { get; set; } = true;

    public bool EnableMemberSuspensionCronJob { get; set; } = true;

    public bool EnableHealthCheckEndpoint { get; set; } = true;

    public bool EnableLogDownloadEndpoint { get; set; } = true;

    /// <summary>Emit OpenTelemetry traces + metrics over OTLP (e.g. to Jaeger).</summary>
    public bool EnableOpenTelemetry { get; set; }

    /// <summary>OTLP gRPC endpoint. Default targets a local collector / Jaeger.</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>Reported as the OpenTelemetry <c>service.name</c> resource attribute.</summary>
    public string ServiceName { get; set; } = "Library.Api";

    /// <summary>Per-client fixed-window rate limiting (keyed by forwarded/remote IP).</summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>Requests allowed per <see cref="RateLimitWindowSeconds"/> per client.</summary>
    public int RateLimitPermitPerWindow { get; set; } = 120;

    public int RateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Serve the OpenAPI document (<c>/openapi/v1.json</c>) and the Scalar API
    /// reference UI (<c>/scalar</c>). On by default in every environment so the
    /// dockerised (Production) stack still exposes the API console; set to
    /// <c>false</c> to hide it in a hardened deployment.
    /// </summary>
    public bool EnableApiReference { get; set; } = true;
}
