namespace Library.Application.Common.Logging;

/// <summary>
/// Writes structured log entries to the appropriate log stream.
/// Implementations must never throw - a logging failure must not be
/// allowed to take down the request/job that triggered it.
/// </summary>
public interface IAppLogWriter
{
    Task WriteAsync(
        AppLogEntry entry,
        CancellationToken cancellationToken = default);
}
