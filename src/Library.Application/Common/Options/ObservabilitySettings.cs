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
}
