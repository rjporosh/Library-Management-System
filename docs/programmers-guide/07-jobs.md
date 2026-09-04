# Background Jobs & Cron

## The one job today

`src/Library.Api/BackgroundJobs/MemberSuspensionCronJob.cs` — a plain
`BackgroundService` that runs at **local midnight**, delegating to
`MemberMaintenanceService.RunAsync`:

1. suspend members with an overdue active borrow;
2. mark active members whose `MembershipExpiresAt` has passed as `Inactive`.

It is fully wrapped in try/catch — a fault is written to
`runtime-error-logs/` with `CronJobName` set and the loop continues. Toggle with
`FeatureFlags.EnableMemberSuspensionCronJob`; when off it logs once and exits
(no idle timers).

## Manual trigger

`POST /api/jobs/member-maintenance/run` runs the exact same
`MemberMaintenanceService` and returns `{ overdueSuspended, expiredDeactivated,
ranAtUtc }`. `403` when the feature flag is off. The Dashboard "Run membership
maintenance" button calls this.

## Adding a job

1. Put the logic in an **Application service** (so a manual endpoint can share it).
2. Add a `BackgroundService` in `BackgroundJobs/` that resolves the service in a
   DI scope, wraps `RunOnceAsync` in try/catch → `IAppLogWriter` RuntimeError,
   and schedules with `Task.Delay`. Register with `AddHostedService<T>()`.
3. Add a `POST /api/jobs/<name>/run` action to `JobsController`.
4. Gate both behind a `FeatureFlags` flag.

### Cron expressions

This project uses a hand-rolled midnight delay, not a cron library. If you need
real cron scheduling add `Quartz` / `NCrontab` and parse the expression in the
`BackgroundService` — keep the "logic lives in an Application service" rule.
