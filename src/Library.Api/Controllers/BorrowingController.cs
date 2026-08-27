using Library.Application.Features.Borrowing;
using Library.Application.Features.Borrowing.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/borrowing")]
public sealed class BorrowingController(
    BorrowingService borrowingService) : ControllerBase
{
    /// <summary>
    /// Issues a book copy to a library member.
    /// </summary>
    /// <remarks>
    /// The member must be active and the selected book copy must be available.
    /// A new borrow record is created and the book copy is marked as borrowed.
    /// </remarks>
    /// <param name="request">
    /// The member, book copy and due date information required to issue the book.
    /// </param>
    /// <response code="201">
    /// The book was successfully issued and a borrow record was created.
    /// </response>
    /// <response code="400">
    /// The request is invalid, the member cannot borrow, or the due date is invalid.
    /// </response>
    /// <response code="404">
    /// The specified member or book copy was not found.
    /// </response>
    [HttpPost("issue")]
    [ProducesResponseType(
        StatusCodes.Status201Created,
        Type = typeof(BorrowRecordResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BorrowRecordResponse>> Issue(
        IssueBookRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await borrowingService.IssueAsync(
                request,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                record);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    /// <summary>
    /// Returns a borrowed book copy to the library.
    /// </summary>
    /// <remarks>
    /// Marks the borrow record as returned and makes the associated
    /// book copy available again.
    /// </remarks>
    /// <param name="borrowRecordId">
    /// The unique identifier of the borrow record.
    /// </param>
    /// <param name="request">
    /// Optional return information. If no return time is supplied,
    /// the current UTC time is used.
    /// </param>
    /// <response code="200">
    /// The book was successfully returned.
    /// </response>
    /// <response code="400">
    /// The book cannot be returned because the borrow record or book copy
    /// is not in a valid state.
    /// </response>
    /// <response code="404">
    /// The specified borrow record or book copy was not found.
    /// </response>
    [HttpPost("{borrowRecordId:guid}/return")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(BorrowRecordResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BorrowRecordResponse>> Return(
        Guid borrowRecordId,
        ReturnBookRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await borrowingService.ReturnAsync(
                borrowRecordId,
                request,
                cancellationToken);

            return Ok(record);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}