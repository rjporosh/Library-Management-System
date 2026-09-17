using Library.Api.Common;
using Library.Api.Contracts;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Features.BorrowRequests;
using Library.Application.Features.BorrowRequests.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>
/// Member self-service borrow/purchase requests and the librarian approval
/// queue. A Member can only create requests and see their own; a Librarian
/// sees and decides every request.
/// </summary>
[ApiController]
[Route("api/borrow-requests")]
[Authorize]
public sealed class BorrowRequestsController(BorrowRequestService borrowRequestService) : ControllerBase
{
    /// <summary>Creates a borrow or purchase-suggestion request for the signed-in member.</summary>
    /// <response code="201">The created request.</response>
    /// <response code="422">Validation failed, or a pending request for this book already exists.</response>
    [Authorize(Roles = "Member")]
    [HttpPost]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Create(CreateBorrowRequestRequest request, CancellationToken cancellationToken)
    {
        var memberId = this.CurrentMemberId();
        if (memberId is null)
        {
            return Forbid();
        }

        return (await borrowRequestService.CreateAsync(memberId.Value, request, cancellationToken))
            .ToCreatedResult(this, nameof(Mine), new { });
    }

    /// <summary>The signed-in member's own requests, most recent first.</summary>
    /// <response code="200">The member's requests.</response>
    [Authorize(Roles = "Member")]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<BorrowRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Mine(CancellationToken cancellationToken)
    {
        var memberId = this.CurrentMemberId();
        if (memberId is null)
        {
            return Forbid();
        }

        return Ok(await borrowRequestService.GetByMemberIdAsync(memberId.Value, cancellationToken));
    }

    /// <summary>Librarian approval queue: advanced multi-field search across every request.</summary>
    /// <response code="200">A page of matching requests.</response>
    [Authorize(Roles = "Librarian")]
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BorrowRequestResponse>), StatusCodes.Status200OK)]
    public ActionResult Search([FromBody] SearchRequestDto request) =>
        borrowRequestService.Search(request.ToDomain()).ToActionResult(this);

    /// <summary>Approves a request. A Borrow request is issued immediately if a copy is available.</summary>
    /// <response code="200">The approved (or fulfilled) request.</response>
    /// <response code="404">No request with that id.</response>
    /// <response code="409">Already decided, or no copy is currently available.</response>
    [Authorize(Roles = "Librarian")]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        (await borrowRequestService.ApproveAsync(id, this.CurrentUserId(), cancellationToken)).ToActionResult(this);

    /// <summary>Rejects a request.</summary>
    /// <response code="200">The rejected request.</response>
    /// <response code="404">No request with that id.</response>
    /// <response code="409">Already decided.</response>
    [Authorize(Roles = "Librarian")]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(BorrowRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Reject(Guid id, CancellationToken cancellationToken) =>
        (await borrowRequestService.RejectAsync(id, this.CurrentUserId(), cancellationToken)).ToActionResult(this);
}
