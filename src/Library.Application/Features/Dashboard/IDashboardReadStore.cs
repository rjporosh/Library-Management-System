namespace Library.Application.Features.Dashboard;

/// <summary>
/// Read-only source for the dashboard aggregate. Two implementations: EF Core
/// (default) and Dapper (<c>Database:Orm=Dapper</c>) - the Dapper one runs the
/// counts as a single set of SQL aggregates rather than materialising rows.
/// </summary>
public interface IDashboardReadStore
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
