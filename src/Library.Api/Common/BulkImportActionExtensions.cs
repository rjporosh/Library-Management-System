using Library.Api.Middleware;
using Library.Application.Common.Errors;
using Library.Application.Features.BulkImport;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Common;

public static class BulkImportActionExtensions
{
    /// <summary>200 with the imported count on success; 422 with every row error on failure.</summary>
    public static ActionResult ToActionResult(this BulkImportOutcome outcome, ControllerBase controller)
    {
        if (outcome.Succeeded)
        {
            return controller.Ok(new { success = true, imported = outcome.Imported });
        }

        var correlationId = controller.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id)
            ? id?.ToString()
            : null;

        return controller.UnprocessableEntity(new BulkImportErrorResponse(
            false, outcome.Imported, outcome.Truncated, outcome.Errors, correlationId));
    }
}

/// <summary>Failure envelope for a bulk import - the standard error list plus import context.</summary>
public sealed record BulkImportErrorResponse(
    bool Success,
    int Imported,
    bool Truncated,
    IReadOnlyList<ApiError> Errors,
    string? CorrelationId);
