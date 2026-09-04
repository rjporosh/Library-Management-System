using Library.Application.Features.Borrowing;
using Library.Application.Features.Borrowing.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Issue and return workflows.</summary>
[ApiController]
[Route("api/borrowing")]
public sealed class BorrowingController(BorrowingService borrowingService) : ControllerBase
{
    /// <summary>
    /// Issues a book copy to a member. The member must be active (not
    /// suspended/inactive/expired) with no other active borrow, and the
    /// copy must be available.
    /// </summary>
    /// <response code="201">The book was issued and a borrow record created.</response>
    /// <response code="404">The member or copy was not found.</response>
    /// <response code="409">The member cannot borrow, already has an active borrow, or the copy is unavailable.</response>
    /// <response code="422">The request payload is invalid (e.g. a past due date).</response>
    [HttpPost("issue")]
    [ProducesResponseType(typeof(BorrowRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BorrowRecordResponse>> Issue(IssueBookRequest request, CancellationToken cancellationToken)
    {
        var record = await borrowingService.IssueAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, record);
    }

    /// <summary>Returns a borrowed copy and closes its borrow record.</summary>
    /// <response code="200">The book was returned.</response>
    /// <response code="404">The borrow record or copy was not found.</response>
    /// <response code="409">The borrow record or copy is not in a returnable state.</response>
    [HttpPost("{borrowRecordId:guid}/return")]
    [ProducesResponseType(typeof(BorrowRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BorrowRecordResponse>> Return(
        Guid borrowRecordId, ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var record = await borrowingService.ReturnAsync(borrowRecordId, request, cancellationToken);
        return Ok(record);
    }
}
