using Library.Api.Common;
using Library.Api.Contracts;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Features.BulkImport;
using Library.Application.Features.Members;
using Library.Application.Features.Members.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Library member management: enrolment, profile edits, search and lifecycle.</summary>
[ApiController]
[Route("api/members")]
public sealed class MembersController(MemberService memberService, BulkImportService bulkImport) : ControllerBase
{
    /// <summary>Downloads the Excel template for bulk member import.</summary>
    /// <response code="200">The .xlsx template.</response>
    [HttpGet("import/template")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult DownloadImportTemplate()
    {
        var (content, fileName) = bulkImport.MemberTemplate();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>Bulk-imports members from an .xlsx file (all-or-nothing; every error returned with its row).</summary>
    /// <response code="200"><c>{ success, imported }</c>.</response>
    /// <response code="422">Nothing imported - the body lists every error.</response>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BulkImportErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var outcome = await bulkImport.ImportMembersAsync(stream, file.Length, cancellationToken);
        return outcome.ToActionResult(this);
    }

    /// <summary>Advanced multi-field search for members.</summary>
    /// <remarks>
    /// POST a filter set (field / operator / value), an AND/OR match mode, multi-field sort and paging.
    /// Status is matched by name, e.g. <c>{ "field": "status", "operator": "eq", "value": "Suspended" }</c>.
    /// </remarks>
    /// <response code="200">A page of matching members.</response>
    /// <response code="400">A filter references an unknown field, operator or unparseable value.</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<MemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult Search([FromBody] SearchRequestDto request) =>
        memberService.Search(request.ToDomain()).ToActionResult(this);

    /// <summary>Lists members (quick text search + paging). Use <c>POST /search</c> for advanced filters.</summary>
    /// <response code="200">A page of members.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MemberResponse>), StatusCodes.Status200OK)]
    public ActionResult List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var dto = new SearchRequestDto { Search = search, Page = page, PageSize = pageSize };

        if (!string.IsNullOrWhiteSpace(status))
        {
            dto.Filters.Add(new SearchRequestDto.FilterDto { Field = "status", Operator = "eq", Value = status });
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            dto.Sort.Add(new SearchRequestDto.SortDto { Field = sortBy, Direction = sortDirection ?? "asc" });
        }

        return memberService.Search(dto.ToDomain()).ToActionResult(this);
    }

    /// <summary>Retrieves a member by id.</summary>
    /// <response code="200">The member.</response>
    /// <response code="404">No member with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var member = await memberService.GetByIdAsync(id, cancellationToken);
        return member is null ? NotFound() : Ok(member);
    }

    /// <summary>Retrieves a member with their borrowing summary and history.</summary>
    /// <response code="200">The member detail.</response>
    /// <response code="404">No member with that id.</response>
    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(typeof(MemberDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailResponse>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var detail = await memberService.GetDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Enrols a new member (membership starts active with a one-year term).</summary>
    /// <response code="201">The member was created.</response>
    /// <response code="422">The supplied information failed validation - every problem is listed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Create(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await memberService.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), new { id = result.Value?.Id });
    }

    /// <summary>Updates a member's profile fields.</summary>
    /// <response code="200">The updated member.</response>
    /// <response code="404">No member with that id.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Update(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken) =>
        (await memberService.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>Deletes a member. Blocked while they have an active borrow.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No member with that id.</response>
    /// <response code="409">The member has an active borrow.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(Guid id, [FromQuery] bool force = false, CancellationToken cancellationToken = default) =>
        (await memberService.DeleteAsync(id, force, cancellationToken)).ToActionResult(this, StatusCodes.Status204NoContent);

    /// <summary>Suspends a member (same action the nightly job performs for overdue borrowers).</summary>
    /// <response code="200">The suspended member.</response>
    /// <response code="404">No member with that id.</response>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> Suspend(Guid id, CancellationToken cancellationToken) =>
        Ok(await memberService.SuspendAsync(id, cancellationToken));

    /// <summary>Sets a member back to Active without recording a renewal (administrative override).</summary>
    /// <response code="200">The reactivated member.</response>
    /// <response code="404">No member with that id.</response>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await memberService.ReactivateAsync(id, cancellationToken));

    /// <summary>Renews the membership: clears suspension and extends the term by one year.</summary>
    /// <response code="200">The renewed member.</response>
    /// <response code="404">No member with that id.</response>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> Renew(Guid id, CancellationToken cancellationToken) =>
        Ok(await memberService.RenewAsync(id, cancellationToken));

    /// <summary>Marks an active member Inactive (manual equivalent of the membership-expiry sweep).</summary>
    /// <response code="200">The deactivated member.</response>
    /// <response code="404">No member with that id.</response>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await memberService.DeactivateAsync(id, cancellationToken));
}
