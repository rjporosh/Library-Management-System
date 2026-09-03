namespace Library.Application.Common.Logging;

/// <summary>
/// A single structured log line. Every field is nullable except the
/// category and timestamp because not every category populates every
/// field (e.g. a query log has no "root cause").
/// Written as one JSON object per line (JSON Lines) so the files stay
/// both human-readable and machine-parseable for tooling/alerting.
/// </summary>
public sealed record AppLogEntry
{
    public required LogCategory Category { get; init; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Correlation/request id, when the entry originates from an HTTP request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Short human-readable summary of what happened.</summary>
    public string? Message { get; init; }

    /// <summary>Source file the fault/query originated from.</summary>
    public string? FileName { get; init; }

    /// <summary>Full path of <see cref="FileName"/> on disk (best effort - available when PDB symbols are present).</summary>
    public string? FileLocation { get; init; }

    /// <summary>The method/function where the fault or query originated.</summary>
    public string? MethodName { get; init; }

    /// <summary>Populated when the entry was produced by a scheduled/background job.</summary>
    public string? CronJobName { get; init; }

    /// <summary>Line number inside <see cref="FileName"/>, when available.</summary>
    public int? LineNumber { get; init; }

    /// <summary>The underlying exception message / diagnosed cause.</summary>
    public string? RootCause { get; init; }

    /// <summary>A short, actionable suggestion for how to fix the issue.</summary>
    public string? PossibleBestFix { get; init; }

    /// <summary>The exact query/operation that was executed (SQL text, or a description for in-memory operations).</summary>
    public string? GeneratedQuery { get; init; }

    public DateTime? ExecutionStartTimeUtc { get; init; }

    public DateTime? ExecutionEndTimeUtc { get; init; }

    /// <summary>Elapsed time of the query/operation, in milliseconds.</summary>
    public double? QueryElapsedMilliseconds { get; init; }

    /// <summary>Full exception type name, when applicable.</summary>
    public string? ExceptionType { get; init; }

    /// <summary>Full stack trace, when applicable.</summary>
    public string? StackTrace { get; init; }
}
