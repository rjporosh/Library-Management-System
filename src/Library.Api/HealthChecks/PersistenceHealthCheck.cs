using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Options;
using Library.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Library.Api.HealthChecks;

/// <summary>
/// Verifies the persistence layer is reachable. For a relational provider this
/// opens a connection (<c>CanConnectAsync</c>); when it fails the description
/// names the provider and database so a monitor / load balancer sees the cause.
/// The in-memory provider just confirms the repository responds.
/// </summary>
public sealed class PersistenceHealthCheck(
    IServiceProvider services,
    DatabaseOptions database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (database.IsRelational)
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy($"{database.Provider} database is reachable.")
                    : HealthCheckResult.Unhealthy(
                        $"Cannot connect to the {database.Provider} database. " +
                        "Check the server is running and Database:ConnectionString is correct.");
            }

            var repo = services.GetRequiredService<IBookRepository>();
            _ = await repo.GetByIdAsync(Guid.Empty, cancellationToken);
            return HealthCheckResult.Healthy("In-memory persistence is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Persistence layer ({database.Provider}) is not reachable: {ex.Message}", ex);
        }
    }
}
