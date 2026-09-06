using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Library.Application.Common.Errors;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Logging;

namespace Library.Api.Middleware;

/// <summary>
/// Central exception handler for the whole request pipeline.
///
/// Two outcomes:
///  1) Expected/anticipated exceptions (KeyNotFoundException,
///     ArgumentException, InvalidOperationException) are mapped to a
///     clean 4xx response using the standard error envelope and
///     logged to exception-logs. These are business-rule outcomes,
///     not application faults - "expected validation failures must
///     not become 500 errors" (ROADMAP Phase 9).
///  2) Anything else is unexpected: it is logged in full (stack
///     trace, file/line when symbols are available) to
///     runtime-error-logs, and the client only ever sees a safe,
///     generic message - never internal exception details.
///
/// This middleware is additive: existing controllers that already
/// catch and translate their own exceptions (e.g. BorrowingController)
/// are untouched and continue to behave exactly as before. This is
/// the safety net for everything else, and the single place new
/// features should rely on going forward.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    IAppLogWriter logWriter,
    Microsoft.Extensions.Hosting.IHostEnvironment environment,
    Microsoft.Extensions.Localization.IStringLocalizer<Library.Api.SharedResources> messages,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.Items.TryGetValue(
            CorrelationIdMiddleware.ItemKey,
            out var id)
            ? id?.ToString()
            : null;

        var (statusCode, errorCode, isExpected) = Classify(ex);

        // Validation failures carry a full list of field errors - surface
        // every one of them together (spec §6.3), not just ex.Message.
        if (ex is ValidationException validation)
        {
            await logWriter.WriteAsync(new AppLogEntry
            {
                Category = LogCategory.Exception,
                CorrelationId = correlationId,
                Message = ex.Message,
                ExceptionType = ex.GetType().FullName,
                RootCause = string.Join("; ", validation.Errors.Select(e => $"{e.Field}:{e.ErrorCode}")),
                PossibleBestFix = "Correct the listed fields and resubmit; each error names the field, rule and accepted values."
            });

            if (!context.Response.HasStarted)
            {
                await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity,
                    "Unprocessable Entity", messages["Error.Validation"], validation.Errors, correlationId, ex);
            }

            return;
        }

        var stackTrace = new StackTrace(ex, fNeedFileInfo: true);
        var frame = stackTrace.GetFrame(0);

        await logWriter.WriteAsync(new AppLogEntry
        {
            Category = isExpected ? LogCategory.Exception : LogCategory.RuntimeError,
            CorrelationId = correlationId,
            Message = ex.Message,
            ExceptionType = ex.GetType().FullName,
            MethodName = frame?.GetMethod()?.Name ?? ex.TargetSite?.Name,
            FileName = frame?.GetFileName() is { } f ? Path.GetFileName(f) : null,
            FileLocation = frame?.GetFileName(),
            LineNumber = frame?.GetFileLineNumber() is > 0 ? frame.GetFileLineNumber() : null,
            RootCause = ex.InnerException?.Message ?? ex.Message,
            PossibleBestFix = SuggestFix(ex),
            StackTrace = ex.StackTrace
        });

        // Also emit to the standard ASP.NET Core logger so it shows up
        // in the console/host logs alongside everything else.
        logger.LogError(
            ex,
            "Unhandled exception (correlationId={CorrelationId}, expected={IsExpected})",
            correlationId,
            isExpected);

        if (context.Response.HasStarted)
            return;

        var localizedGeneric = statusCode switch
        {
            (int)HttpStatusCode.NotFound => messages["Error.NotFound"],
            (int)HttpStatusCode.Conflict => messages["Error.Conflict"],
            (int)HttpStatusCode.BadRequest => messages["Error.Validation"],
            _ => messages["Error.Unexpected"],
        };

        var clientMessage = isExpected ? ex.Message : localizedGeneric.Value;
        var title = statusCode switch
        {
            (int)HttpStatusCode.NotFound => "Not Found",
            (int)HttpStatusCode.Conflict => "Conflict",
            (int)HttpStatusCode.BadRequest => "Bad Request",
            (int)HttpStatusCode.UnprocessableEntity => "Unprocessable Entity",
            _ => "Internal Server Error",
        };

        await WriteProblemAsync(context, statusCode, title, clientMessage,
            [new ApiError(errorCode, clientMessage)], correlationId, ex);
    }

    /// <summary>Writes an RFC 7807 problem+json body with our envelope fields as extension members.</summary>
    private async Task WriteProblemAsync(
        HttpContext context, int status, string title, string detail,
        IReadOnlyList<ApiError> errors, string? correlationId, Exception ex)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        object body = new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail,
            instance = context.Request.Path.Value,
            success = false,
            errors,
            correlationId,
        };

        if (environment.IsDevelopment())
        {
            body = new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                detail,
                instance = context.Request.Path.Value,
                success = false,
                errors,
                correlationId,
                debug = new { exceptionType = ex.GetType().FullName, ex.StackTrace },
            };
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, WebJson));
    }

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static (int StatusCode, string ErrorCode, bool IsExpected) Classify(Exception ex) => ex switch
    {
        ValidationException => ((int)HttpStatusCode.UnprocessableEntity, "VALIDATION_ERROR", true),
        KeyNotFoundException => ((int)HttpStatusCode.NotFound, "NOT_FOUND", true),
        ArgumentException => ((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", true),
        InvalidOperationException => ((int)HttpStatusCode.Conflict, "CONFLICT", true),
        _ => ((int)HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", false)
    };

    private static string SuggestFix(Exception ex) => ex switch
    {
        KeyNotFoundException => "Verify the identifier exists before referencing it; confirm the caller is using a valid id.",
        ArgumentException => "Review the request payload against the documented field constraints (see /api/release-notes/current or API docs).",
        InvalidOperationException => "The action conflicts with the current entity state (e.g. already borrowed/returned/suspended) - re-check state before retrying.",
        TimeoutException => "Check downstream dependency (database/external service) health via /health; consider retry/backoff.",
        _ => "Inspect the stack trace and inner exception above; check /health for dependency status."
    };
}
