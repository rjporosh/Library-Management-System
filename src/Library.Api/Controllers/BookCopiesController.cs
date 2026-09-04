using Library.Api.Common;
using Library.Api.Contracts;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Features.BookCopies;
using Library.Application.Features.BookCopies.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Physical book-copy management: registration, condition changes, search.</summary>
[ApiController]
[Route("api/book-copies")]
public sealed class BookCopiesController(BookCopyService bookCopyService) : ControllerBase
{
    /// <summary>Advanced multi-field search for book copies (status matched by name).</summary>
    /// <response code="200">A page of matching copies.</response>
    /// <response code="400">A filter references an unknown field/operator/value.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<BookCopyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult Search([FromBody] SearchRequestDto request) =>
        bookCopyService.Search(request.ToDomain()).ToActionResult(this);

    /// <summary>Lists copies with a quick text search and optional filters.</summary>
    /// <response code="200">A page of copies.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookCopyResponse>), StatusCodes.Status200OK)]
    public ActionResult List(
        [FromQuery] string? search = null,
        [FromQuery] Guid? bookId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var dto = new SearchRequestDto { Search = search, Page = page, PageSize = pageSize };

        if (bookId is { } b)
        {
            dto.Filters.Add(new SearchRequestDto.FilterDto { Field = "bookId", Operator = "eq", Value = b.ToString() });
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            dto.Filters.Add(new SearchRequestDto.FilterDto { Field = "status", Operator = "eq", Value = status });
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            dto.Sort.Add(new SearchRequestDto.SortDto { Field = sortBy, Direction = sortDirection ?? "asc" });
        }

        return bookCopyService.Search(dto.ToDomain()).ToActionResult(this);
    }

    /// <summary>Retrieves all copies of a specific book.</summary>
    /// <response code="200">The book's copies.</response>
    [HttpGet("book/{bookId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<BookCopyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookCopyResponse>>> GetByBookId(Guid bookId, CancellationToken cancellationToken) =>
        Ok(await bookCopyService.GetByBookIdAsync(bookId, cancellationToken));

    /// <summary>Retrieves a single copy by id.</summary>
    /// <response code="200">The copy.</response>
    /// <response code="404">No copy with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookCopyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookCopyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var copy = await bookCopyService.GetByIdAsync(id, cancellationToken);
        return copy is null ? NotFound() : Ok(copy);
    }

    /// <summary>Registers a new physical copy of a book.</summary>
    /// <response code="201">The copy was created.</response>
    /// <response code="409">The barcode is already in use.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BookCopyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Create(CreateBookCopyRequest request, CancellationToken cancellationToken)
    {
        var result = await bookCopyService.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Value?.Id });
    }

    /// <summary>Corrects a copy's barcode.</summary>
    /// <response code="200">The updated copy.</response>
    /// <response code="404">No copy with that id.</response>
    /// <response code="409">The barcode is already in use.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BookCopyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(Guid id, UpdateBookCopyRequest request, CancellationToken cancellationToken) =>
        (await bookCopyService.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>Changes a copy's condition (Available / Lost / Damaged / Maintenance).</summary>
    /// <response code="200">The updated copy.</response>
    /// <response code="404">No copy with that id.</response>
    /// <response code="409">The copy is currently borrowed.</response>
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(BookCopyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ChangeStatus(Guid id, ChangeBookCopyStatusRequest request, CancellationToken cancellationToken) =>
        (await bookCopyService.ChangeStatusAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>Deletes a copy. Blocked while it is borrowed.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No copy with that id.</response>
    /// <response code="409">The copy is currently borrowed.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await bookCopyService.DeleteAsync(id, cancellationToken)).ToActionResult(this, StatusCodes.Status204NoContent);
}
