using Library.Application.Common.Logging;
using Library.Application.Common.Options;
using Library.Application.Features.Members;

namespace Library.Api.BackgroundJobs;

/// <summary>
/// Runs once every day at 00:00 (midnight, local time) and suspends
/// every member who has an active borrow whose due date has already
/// passed. Staff can bring a suspended member back with
/// MembersController's /reactivate or /renew endpoints.
///
/// Toggle: FeatureFlags.EnableMemberSuspensionCronJob (appsettings.json).
/// When disabled, the job logs once and exits without scheduling any
/// further work - no timers/threads are left running.
///
/// Fully wrapped in try/catch: a fault here is logged to
/// runtime-error-logs (with CronJobName populated) and the loop
/// continues on the next scheduled tick - a single bad run must never
/// stop future runs or crash the host.
/// </summary>
public sealed class MemberSuspensionCronJob(
    IServiceScopeFactory scopeFactory,
    ObservabilitySettings settings,
    IAppLogWriter logWriter,
    ILogger<MemberSuspensionCronJob> logger) : BackgroundService
{
    private const string JobName = "MemberSuspensionCronJob";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.EnableMemberSuspensionCronJob)
        {
            logger.LogInformation(
                "{JobName} is disabled via FeatureFlags.EnableMemberSuspensionCronJob.",
                JobName);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextMidnight();

            logger.LogInformation(
                "{JobName} scheduled to run in {Delay} (next midnight).",
                JobName,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunOnceAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Executes a single suspension pass. Public/internal-testable
    /// entry point so this doesn't require waiting for real midnight
    /// in tests - call directly with a scope in a test host.
    /// </summary>
    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var startedUtc = DateTime.UtcNow;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var maintenance = scope.ServiceProvider
                .GetRequiredService<MemberMaintenanceService>();

            await using var queryScope = QueryLogScope.Begin(
                logWriter,
                methodName: nameof(RunOnceAsync),
                generatedQuery:
                    "SCAN borrow_records WHERE status = Active AND due_at < @now; " +
                    "SCAN members WHERE status = Active AND membership_expires_at < @now",
                cronJobName: JobName,
                enabled: settings.EnableQueryLogging);

            var result = await maintenance.RunAsync(cancellationToken);

            logger.LogInformation(
                "{JobName} completed: {Suspended} member(s) suspended (overdue), {Deactivated} member(s) marked Inactive (expired).",
                JobName,
                result.OverdueSuspended,
                result.ExpiredDeactivated);
        }
        catch (Exception ex)
        {
            await logWriter.WriteAsync(new AppLogEntry
            {
                Category = LogCategory.RuntimeError,
                CronJobName = JobName,
                MethodName = nameof(RunOnceAsync),
                Message = ex.Message,
                ExceptionType = ex.GetType().FullName,
                RootCause = ex.InnerException?.Message ?? ex.Message,
                PossibleBestFix =
                    "Check that member/borrow-record persistence is reachable; " +
                    "verify the job's dependencies via /health.",
                StackTrace = ex.StackTrace,
                ExecutionStartTimeUtc = startedUtc,
                ExecutionEndTimeUtc = DateTime.UtcNow
            }, cancellationToken);

            logger.LogError(ex, "{JobName} failed.", JobName);
        }
    }

    private static TimeSpan GetDelayUntilNextMidnight()
    {
        var now = DateTime.Now;
        var nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }
}
