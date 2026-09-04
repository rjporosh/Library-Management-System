using Library.Api.Common;
using Library.Api.Contracts;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Features.Books;
using Library.Application.Features.Books.Models;
using Library.Application.Features.BulkImport;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BooksController(BookService bookService, BulkImportService bulkImport) : ControllerBase
{
    /// <summary>Downloads the Excel template for bulk book import (headers, examples, instructions sheet).</summary>
    /// <response code="200">The .xlsx template.</response>
    [HttpGet("import/template")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult DownloadImportTemplate()
    {
        var (content, fileName) = bulkImport.BookTemplate();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Bulk-imports books from an .xlsx file. All-or-nothing: if any row is
    /// invalid, malformed, or a duplicate (in the file or the database),
    /// nothing is imported and every error is returned with its exact Excel
    /// row, field and accepted values.
    /// </summary>
    /// <response code="200">Every row imported. Body: <c>{ success, imported }</c>.</response>
    /// <response code="422">Validation failed - nothing was written. Body lists every error.</response>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BulkImportErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var outcome = await bulkImport.ImportBooksAsync(stream, file.Length, cancellationToken);
        return outcome.ToActionResult(this);
    }

    /// <summary>Advanced multi-field search for the book catalog.</summary>
    /// <remarks>
    /// POST a filter set (field / operator / value(s)), an AND/OR match mode, multi-field sort and paging.
    /// Operators: eq, neq, contains, notContains, startsWith, endsWith, gt, gte, lt, lte, in, notIn, between.
    /// Example: filter title contains "clean" OR author contains "martin", sorted by publishedYear desc.
    /// </remarks>
    /// <response code="200">A page of matching books.</response>
    /// <response code="400">A filter references an unknown field, operator or unparseable value.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult Search([FromBody] SearchRequestDto request) =>
        bookService.Search(request.ToDomain()).ToActionResult(this);

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

        /// <summary>
    /// Updates an existing book in the library catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the book.</param>
    /// <param name="request">The updated book information.</param>
    /// <response code="200">The book was successfully updated.</response>
    /// <response code="404">No book exists with the specified identifier.</response>
    /// <response code="400">The supplied book information is invalid.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(BookResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookResponse>> Update(
        Guid id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await bookService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return book is null
            ? NotFound()
            : Ok(book);
    }

    /// <summary>
    /// Deletes an existing book from the library catalog.
    /// </summary>
    /// <param name="id">The unique identifier of the book.</param>
    /// <response code="204">The book was successfully deleted.</response>
    /// <response code="404">No book exists with the specified identifier.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await bookService.DeleteAsync(
            id,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}