using System.Globalization;
using Library.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Library.Api.Controllers;

/// <summary>
/// Reference data the front-end uses to render status badges, build the
/// advanced-search UI and localize error messages without hard-coding.
/// </summary>
[ApiController]
[Route("api/metadata")]
public sealed class MetadataController(IStringLocalizer<SharedResources> messages) : ControllerBase
{
    /// <summary>Every status enum and its allowed values, by name.</summary>
    [HttpGet("enums")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyDictionary<string, string[]>> GetEnums() =>
        Ok(new Dictionary<string, string[]>
        {
            ["memberStatus"] = Enum.GetNames<MemberStatus>(),
            ["bookCopyStatus"] = Enum.GetNames<BookCopyStatus>(),
            ["borrowStatus"] = Enum.GetNames<BorrowStatus>(),
        });

    /// <summary>The operators the advanced-search endpoints accept.</summary>
    [HttpGet("search-operators")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string[]> GetSearchOperators() =>
        Ok(new[]
        {
            "eq", "neq", "contains", "notContains", "startsWith", "endsWith",
            "gt", "gte", "lt", "lte", "in", "notIn", "between",
        });

    /// <summary>Supported UI languages (default first).</summary>
    [HttpGet("languages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetLanguages() =>
        Ok(new
        {
            @default = "en",
            supported = new[]
            {
                new { code = "en", name = "English" },
                new { code = "bn", name = "বাংলা (Bangla)" },
            },
        });

    /// <summary>
    /// The localized text for every error code / system message in the current
    /// culture (from Accept-Language or <c>?culture=</c>). The front-end maps an
    /// <c>ApiError.errorCode</c> to the message here so the whole error contract
    /// is localizable without server changes.
    /// </summary>
    [HttpGet("messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetMessages()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var map = new Dictionary<string, string>();

        foreach (var entry in messages.GetAllStrings(includeParentCultures: true))
        {
            map[entry.Name] = entry.Value;
        }

        return Ok(new { culture, messages = map });
    }
}
