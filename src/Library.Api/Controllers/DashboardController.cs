using Library.Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Live aggregate figures for the librarian dashboard.</summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Returns catalogue, copy, member and borrowing totals plus recent
    /// borrowing activity in a single call (no N+1).
    /// </summary>
    /// <response code="200">The dashboard snapshot.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardSnapshot), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSnapshot>> Get(CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetSnapshotAsync(cancellationToken));
}
