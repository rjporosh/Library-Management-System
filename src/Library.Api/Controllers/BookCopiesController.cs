using Library.Application.Features.BookCopies;
using Library.Application.Features.BookCopies.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/book-copies")]
public sealed class BookCopiesController(
    BookCopyService bookCopyService) : ControllerBase
{
    /// <summary>
    /// Retrieves all copies of a specific book.
    /// </summary>
    /// <param name="bookId">The unique identifier of the book.</param>
    /// <response code="200">The book copies were retrieved successfully.</response>
    [HttpGet("book/{bookId:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IReadOnlyList<BookCopyResponse>))]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> GetByBookId(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var copies = await bookCopyService.GetByBookIdAsync(
            bookId,
            cancellationToken);

        return Ok(copies);
    }

    /// <summary>
    /// Retrieves a single book copy by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the book copy.</param>
    /// <response code="200">The book copy was found.</response>
    /// <response code="404">The book copy was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(BookCopyResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookCopyResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var copy = await bookCopyService.GetByIdAsync(
            id,
            cancellationToken);

        return copy is null
            ? NotFound()
            : Ok(copy);
    }

    /// <summary>
    /// Adds a new physical copy of a book to the library.
    /// </summary>
    /// <param name="request">The book copy information to create.</param>
    /// <response code="201">The book copy was successfully created.</response>
    /// <response code="400">The supplied book copy information is invalid.</response>
    [HttpPost]
    [ProducesResponseType(
        StatusCodes.Status201Created,
        Type = typeof(BookCopyResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookCopyResponse>> Create(
        CreateBookCopyRequest request,
        CancellationToken cancellationToken)
    {
        var copy = await bookCopyService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = copy.Id },
            copy);
    }
}