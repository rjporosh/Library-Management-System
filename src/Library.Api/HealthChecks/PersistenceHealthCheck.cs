using Library.Application.Abstractions.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Library.Api.HealthChecks;

/// <summary>
/// Verifies the persistence layer can be reached. Today's
/// implementation (in-memory repositories) can never actually be
/// "down", so this simply confirms the repository is resolvable and
/// responsive - the same check will become a real
/// `SELECT 1`/connection-open ping once a relational provider
/// (Phase 6) is introduced, without changing this check's contract
/// or the /health response shape.
/// </summary>
public sealed class PersistenceHealthCheck(
    IBookRepository bookRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await bookRepository.GetByIdAsync(Guid.Empty, cancellationToken);

            return HealthCheckResult.Healthy(
                "Persistence layer is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Persistence layer is not reachable.",
                ex);
        }
    }
}
