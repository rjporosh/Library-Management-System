using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Library.Application.Common.Errors;
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
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private const string SupportMessage =
        "Something went wrong. Please contact service provider " +
        "MD. IKRAMUL ISLAM SIDDIQUE POROSH, phone: +8801672896992 for details.";

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

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var responseMessage = isExpected
            ? ex.Message
            : SupportMessage;

        var payload = ApiErrorResponse.Single(
            new ApiError(errorCode, responseMessage),
            correlationId);

        // In Development, include diagnostic detail to speed up
        // debugging without ever exposing it outside Development.
        var json = environment.IsDevelopment()
            ? JsonSerializer.Serialize(new
            {
                payload.Success,
                payload.Errors,
                payload.CorrelationId,
                debug = new
                {
                    exceptionType = ex.GetType().FullName,
                    ex.StackTrace
                }
            })
            : JsonSerializer.Serialize(payload);

        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string ErrorCode, bool IsExpected) Classify(Exception ex) => ex switch
    {
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
