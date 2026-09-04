using Library.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>
/// Reference data the front-end uses to render status badges and build the
/// advanced-search UI without hard-coding enum names.
/// </summary>
[ApiController]
[Route("api/metadata")]
public sealed class MetadataController : ControllerBase
{
    /// <summary>Every status enum and its allowed values, by name.</summary>
    /// <response code="200">The enum reference data.</response>
    [HttpGet("enums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyDictionary<string, string[]>> GetEnums()
    {
        var payload = new Dictionary<string, string[]>
        {
            ["memberStatus"] = Enum.GetNames<MemberStatus>(),
            ["bookCopyStatus"] = Enum.GetNames<BookCopyStatus>(),
            ["borrowStatus"] = Enum.GetNames<BorrowStatus>()
        };

        return Ok(payload);
    }

    /// <summary>The operators the advanced-search endpoints accept.</summary>
    /// <response code="200">The supported filter operators.</response>
    [HttpGet("search-operators")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string[]> GetSearchOperators() =>
        Ok(new[]
        {
            "eq", "neq", "contains", "notContains", "startsWith", "endsWith",
            "gt", "gte", "lt", "lte", "in", "notIn", "between"
        });
}
