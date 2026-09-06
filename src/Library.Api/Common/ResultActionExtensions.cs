using Library.Api.Middleware;
using Library.Application.Common.Errors;
using Library.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Common;

/// <summary>
/// Maps an application <see cref="Result"/> onto an HTTP response using the
/// standard <see cref="ApiErrorResponse"/> envelope for failures. Success
/// bodies stay as raw DTOs (the project has not adopted a success envelope -
/// see the plan / ADR).
/// </summary>
public static class ResultActionExtensions
{
    public static ActionResult ToActionResult(this Result result, ControllerBase controller, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode == StatusCodes.Status204NoContent
                ? controller.NoContent()
                : controller.StatusCode(successStatusCode);
        }

        return controller.Failure(result.Errors);
    }

    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode == StatusCodes.Status204NoContent
                ? controller.NoContent()
                : controller.StatusCode(successStatusCode, result.Value);
        }

        return controller.Failure(result.Errors);
    }

    /// <summary>201 Created for a successful <see cref="Result{T}"/>, else the mapped failure.</summary>
    public static ActionResult ToCreatedResult<T>(this Result<T> result, ControllerBase controller, string actionName, object routeValues)
    {
        return result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : controller.Failure(result.Errors);
    }

    private static ActionResult Failure(this ControllerBase controller, IReadOnlyList<ApiError> errors)
    {
        var correlationId = controller.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id)
            ? id?.ToString()
            : null;

        var status = StatusFor(errors);
        return controller.StatusCode(status, new ApiErrorResponse(false, errors, correlationId));
    }

    private static int StatusFor(IReadOnlyList<ApiError> errors)
    {
        var code = errors.Count > 0 ? errors[0].ErrorCode : ErrorCodes.ValidationError;

        if (code.EndsWith("_NOT_FOUND", StringComparison.Ordinal))
        {
            return StatusCodes.Status404NotFound;
        }

        if (code.EndsWith("_DUPLICATE", StringComparison.Ordinal)
            || code.EndsWith("_HAS_BORROWED_COPIES", StringComparison.Ordinal)
            || code.EndsWith("_HAS_DEPENDENT_COPIES", StringComparison.Ordinal)
            || code.EndsWith("_HAS_BORROW_HISTORY", StringComparison.Ordinal)
            || code is ErrorCodes.Conflict or ErrorCodes.MemberHasActiveBorrow or ErrorCodes.BookCopyBorrowed)
        {
            return StatusCodes.Status409Conflict;
        }

        if (code is ErrorCodes.FeatureDisabled)
        {
            return StatusCodes.Status403Forbidden;
        }

        // Any error carrying a row number is a bulk-import / entity payload problem.
        return errors.Any(e => e.Line is not null)
            ? StatusCodes.Status422UnprocessableEntity
            : StatusCodes.Status400BadRequest;
    }
}
