using Library.Application.Common.Pagination;
using Library.Application.Features.Books;
using Library.Application.Features.Books.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController(BookService bookService) : ControllerBase
{
      /// <summary>
    /// Retrieves a paginated and optionally    filtered collection of books.
    /// </summary>
    /// <remarks>
    /// Searches the book catalog using a   case-insensitive partial match.
    ///
    /// When no search fields are specified,    the search is performed against
    /// the book title only.
    ///
    /// Multiple search fields can be   supplied as a comma-separated list.
    /// Matching across multiple fields uses    OR semantics.
    ///
    /// Examples:
    /// - /api/books
    /// - /api/books?search=clean
    /// - /api/books?search=martin&amp; searchBy=title,author
    /// - /api/books?pageNumber=2&amp;  pageSize=20
    /// </remarks>
    /// <param name="pageNumber">
    /// The page number. Defaults to the configured     default page number.
    /// </param>
    /// <param name="pageSize">
    /// The number of books per page. Defaults to the   configured default page size.
    /// The maximum page size is controlled by the  pagination configuration.
    /// </param>
    /// <param name="search">Optional   case-insensitive partial search text.</   param>
    /// <param name="searchBy">
    /// Optional comma-separated search     fields: title, author, isbn.
    /// Defaults to title when omitted.
    /// </param>
    /// <response code="200">The paginated  book collection was retrieved    successfully.</response>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(PagedBookResponse))]
    public async    Task<ActionResult<PagedBookResponse>>  GetAll(
    [FromQuery] int pageNumber = PaginationDefaults.       DefaultPageNumber,
    [FromQuery] int pageSize = PaginationDefaults.     DefaultPageSize,
    [FromQuery] string? search = null,
    [FromQuery] string? searchBy = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortDirection = null,
    CancellationToken cancellationToken = default)
    {
        var query = new BookQuery(
            pageNumber,
            pageSize,
            search,
            searchBy,
            sortBy,
            sortDirection);

        var books = await bookService.  GetAllAsync(
            query,
            cancellationToken);

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