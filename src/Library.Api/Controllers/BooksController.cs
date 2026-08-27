using Library.Application.Features.Books;
using Library.Application.Features.Books.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController(BookService bookService) : ControllerBase
{
    /// <summary>
    /// Retrieves all books in the library catalog.
    /// </summary>
    /// <remarks>
    /// Returns the complete collection of books currently available
    /// in the library catalog.
    /// </remarks>
    /// <response code="200">Books were retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IReadOnlyList<BookResponse>))]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var books = await bookService.GetAllAsync(cancellationToken);

        return Ok(books);
    }

    /// <summary>
    /// Retrieves a single book by its unique identifier.
    /// </summary>
    /// <remarks>
    /// Use the book identifier returned by the book catalog.
    /// </remarks>
    /// <param name="id">The unique identifier of the book.</param>
    /// <response code="200">The requested book was found.</response>
    /// <response code="404">No book exists with the specified identifier.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(BookResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var book = await bookService.GetByIdAsync(
            id,
            cancellationToken);

        return book is null
            ? NotFound()
            : Ok(book);
    }

    /// <summary>
    /// Adds a new book to the library catalog.
    /// </summary>
    /// <remarks>
    /// Creates a new book using the supplied ISBN, title, author,
    /// publication year and optional description.
    /// </remarks>
    /// <param name="request">The book information to create.</param>
    /// <response code="201">The book was successfully created.</response>
    /// <response code="400">The supplied book information is invalid.</response>
    [HttpPost]
    [ProducesResponseType(
        StatusCodes.Status201Created,
        Type = typeof(BookResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookResponse>> Create(
        CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await bookService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = book.Id },
            book);
    }
}