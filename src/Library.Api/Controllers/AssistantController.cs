using Library.Api.Common;
using Library.Application.Features.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>
/// The librarian chat assistant: ask about stock counts, most-borrowed
/// titles, and top borrowers. Configurable engine (RuleBased/Anthropic/
/// OpenAI via Chat:Provider) - see docs/guide.md for setup.
/// </summary>
[ApiController]
[Route("api/assistant")]
[Authorize(Roles = "Librarian")]
public sealed class AssistantController(ChatService chatService, SpeechService speechService) : ControllerBase
{
    /// <summary>Asks the assistant a question about the library's catalog, copies or borrowing activity.</summary>
    /// <response code="200">The assistant's answer.</response>
    /// <response code="400">The message was empty.</response>
    /// <response code="403">The assistant is disabled (Chat:Enabled = false).</response>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Chat(ChatRequest request, CancellationToken cancellationToken) =>
        (await chatService.AskAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Server-side speech-to-text via Hugging Face (Speech:Provider=HuggingFace only -
    /// the default Web Speech API path is entirely client-side and never calls this).
    /// </summary>
    /// <response code="200">The transcribed text.</response>
    /// <response code="400">No audio provided, or the configured provider/key is missing.</response>
    [HttpPost("transcribe")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [ProducesResponseType(typeof(TranscriptionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Transcribe(IFormFile audio, CancellationToken cancellationToken)
    {
        await using var stream = audio.OpenReadStream();
        return (await speechService.TranscribeAsync(stream, audio.ContentType, cancellationToken)).ToActionResult(this);
    }
}
