namespace Library.Application.Common.Logging;

/// <summary>
/// The four centrally-managed log streams. Each category is written
/// to its own dated file under the configured logs root
/// (see <see cref="ObservabilitySettings"/> and
/// Library.Infrastructure.Logging.FileAppLogWriter):
///
///   RuntimeError -> logs/runtime-error-logs/runtime-error-logs-dd-MM-yyyy.txt
///   BuildError   -> logs/build-error-logs/build-error-logs-dd-MM-yyyy.txt
///   Query        -> logs/query-logs/query-logs-dd-MM-yyyy.txt
///   Exception    -> logs/exception-logs/exception-logs-dd-MM-yyyy.logs
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// Unexpected/unhandled exceptions that resulted in a 500
    /// response (or an unhandled fault inside a background job).
    /// </summary>
    RuntimeError,

    /// <summary>
    /// Startup/bootstrap failures - the application could not begin
    /// serving requests (e.g. database unreachable, configuration
    /// invalid, dependency injection failure).
    /// </summary>
    BuildError,

    /// <summary>
    /// Executed data-access queries, with timing, so slow queries can
    /// be identified. Populated today by cron/background jobs and by
    /// repositories that opt in; will become the primary read-path
    /// log once EF Core/Dapper land (see ROADMAP Phase 11).
    /// </summary>
    Query,

    /// <summary>
    /// Anticipated/business exceptions the application already knows
    /// how to translate into a clean 4xx response (validation
    /// failures, not-found, conflicting state, etc).
    /// </summary>
    Exception
}
