using System.Data.Common;
using Library.Application.Common.Logging;
using Library.Application.Common.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Library.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes a structured <see cref="LogCategory.Query"/> entry for every EF Core
/// command: the SQL text, parameter names and types (never values), elapsed
/// time, rows affected and the database provider. Gated by
/// <see cref="ObservabilitySettings.EnableQueryLogging"/>.
/// </summary>
public sealed class QueryLoggingInterceptor(
    IAppLogWriter logWriter,
    ObservabilitySettings settings,
    DatabaseOptions database) : DbCommandInterceptor
{
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        await LogAsync(command, eventData, rows: null);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await LogAsync(command, eventData, rows: result);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        await LogAsync(command, eventData, rows: null);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private Task LogAsync(DbCommand command, CommandExecutedEventData eventData, int? rows)
    {
        if (!settings.EnableQueryLogging)
        {
            return Task.CompletedTask;
        }

        var parameters = command.Parameters.Count == 0
            ? null
            : string.Join(", ", command.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName}:{p.DbType}"));

        return logWriter.WriteAsync(new AppLogEntry
        {
            Category = LogCategory.Query,
            GeneratedQuery = command.CommandText,
            MethodName = QueryCallerScope.Current,
            QueryElapsedMilliseconds = eventData.Duration.TotalMilliseconds,
            ExecutionEndTimeUtc = DateTime.UtcNow,
            RootCause = parameters,
            PossibleBestFix = database.Provider,
        });
    }
}

/// <summary>
/// Ambient tag for "which application method issued the current query", set by
/// read stores / services so the query log is traceable without a stack walk.
/// </summary>
public static class QueryCallerScope
{
    private static readonly AsyncLocal<string?> Value = new();

    public static string? Current => Value.Value;

    public static IDisposable Enter(string caller)
    {
        var previous = Value.Value;
        Value.Value = caller;
        return new Pop(previous);
    }

    private sealed class Pop(string? previous) : IDisposable
    {
        public void Dispose() => Value.Value = previous;
    }
}
