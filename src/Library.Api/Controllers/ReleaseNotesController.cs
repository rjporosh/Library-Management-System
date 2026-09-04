using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Exposes the current release for SQA / release verification (spec §19).</summary>
[ApiController]
[Route("api/release-notes")]
public sealed class ReleaseNotesController(IWebHostEnvironment environment) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The current release in a machine-readable form: version, release date,
    /// new features, fixes and the QA checklist.
    /// </summary>
    /// <response code="200">The current release notes.</response>
    /// <response code="404">The release-notes file is missing from the deployment.</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ReleaseNote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReleaseNote>> GetCurrent(CancellationToken cancellationToken)
    {
        var path = Path.Combine(environment.ContentRootPath, "release-notes.json");
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        await using var stream = System.IO.File.OpenRead(path);
        var note = await JsonSerializer.DeserializeAsync<ReleaseNote>(stream, Json, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }
}

public sealed record ReleaseNote(
    string Version,
    string ReleaseDate,
    IReadOnlyList<string> NewFeatures,
    IReadOnlyList<string> Fixed,
    IReadOnlyList<string>? ChangedBehaviour,
    IReadOnlyList<string> QaChecklist,
    IReadOnlyList<string> KnownIssues);
