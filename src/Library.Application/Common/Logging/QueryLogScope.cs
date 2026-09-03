namespace Library.Application.Common.Logging;

/// <summary>
/// Times a data-access/query operation and writes a
/// <see cref="LogCategory.Query"/> entry when disposed, so slow
/// queries can be found from the log files alone (start time, end
/// time, elapsed milliseconds, and the exact query/operation text).
///
/// Usage:
/// <code>
/// await using (queryLogScope.Begin("GetOverdueActiveAsync", "SCAN borrow_records WHERE status = Active AND due_at &lt; @now"))
/// {
///     ... perform the query ...
/// }
/// </code>
/// </summary>
public sealed class QueryLogScope : IAsyncDisposable
{
    private readonly IAppLogWriter _logWriter;
    private readonly string _methodName;
    private readonly string? _generatedQuery;
    private readonly string? _cronJobName;
    private readonly DateTime _startUtc;
    private readonly bool _enabled;

    private QueryLogScope(
        IAppLogWriter logWriter,
        string methodName,
        string? generatedQuery,
        string? cronJobName,
        bool enabled)
    {
        _logWriter = logWriter;
        _methodName = methodName;
        _generatedQuery = generatedQuery;
        _cronJobName = cronJobName;
        _enabled = enabled;
        _startUtc = DateTime.UtcNow;
    }

    public static QueryLogScope Begin(
        IAppLogWriter logWriter,
        string methodName,
        string? generatedQuery = null,
        string? cronJobName = null,
        bool enabled = true)
    {
        return new QueryLogScope(
            logWriter,
            methodName,
            generatedQuery,
            cronJobName,
            enabled);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_enabled)
            return;

        var endUtc = DateTime.UtcNow;

        await _logWriter.WriteAsync(new AppLogEntry
        {
            Category = LogCategory.Query,
            Message = $"{_methodName} executed",
            MethodName = _methodName,
            CronJobName = _cronJobName,
            GeneratedQuery = _generatedQuery,
            ExecutionStartTimeUtc = _startUtc,
            ExecutionEndTimeUtc = endUtc,
            QueryElapsedMilliseconds = (endUtc - _startUtc).TotalMilliseconds
        });
    }
}
