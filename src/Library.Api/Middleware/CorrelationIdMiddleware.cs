namespace Library.Api.Middleware;

/// <summary>
/// Ensures every request carries a correlation id: reuses the
/// inbound "X-Correlation-Id" header when present, otherwise
/// generates one. The id is stored on HttpContext.Items so the
/// exception-handling middleware (and any future logging) can tag
/// every log entry with it, and it is echoed back on the response so
/// a caller/tester can quote it when reporting an issue.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(
            HeaderName,
            out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
