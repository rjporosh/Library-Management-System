using Library.Application.Common.Errors;
using Library.Application.Common.Options;
using Library.Application.Features.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Manual triggers for the scheduled background jobs.</summary>
[ApiController]
[Route("api/jobs")]
[Authorize(Roles = "Librarian")]
public sealed class JobsController(
    MemberMaintenanceService maintenanceService,
    ObservabilitySettings settings) : ControllerBase
{
    /// <summary>
    /// Runs the membership-maintenance pass immediately - the same work the
    /// nightly job does: suspend members with an overdue borrow, and mark
    /// active members whose term has expired as Inactive. A librarian can
    /// use this at any time instead of waiting for midnight.
    /// </summary>
    /// <response code="200">The pass ran. Body: counts + timestamp.</response>
    /// <response code="403">The job is disabled via FeatureFlags.EnableMemberSuspensionCronJob.</response>
    [HttpPost("member-maintenance/run")]
    [ProducesResponseType(typeof(MemberMaintenanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MemberMaintenanceResult>> RunMemberMaintenance(CancellationToken cancellationToken)
    {
        if (!settings.EnableMemberSuspensionCronJob)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiErrorResponse.Single(new ApiError(
                ErrorCodes.FeatureDisabled,
                "The membership-maintenance job is disabled (FeatureFlags.EnableMemberSuspensionCronJob).")));
        }

        var result = await maintenanceService.RunAsync(cancellationToken);
        return Ok(result);
    }
}
