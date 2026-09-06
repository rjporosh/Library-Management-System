using System.Threading.RateLimiting;
using Library.Application.Common.Errors;
using Library.Application.Common.Options;
using Library.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace Library.Api.Infrastructure;

/// <summary>
/// Per-client fixed-window rate limiting. The client key is the first
/// <c>X-Forwarded-For</c> hop (so it works behind a load balancer / nginx)
/// falling back to the remote IP. A throttled request gets HTTP 429 with the
/// standard <see cref="ApiErrorResponse"/> envelope and a <c>Retry-After</c> header.
/// </summary>
public static class RateLimitingExtensions
{
    public const string PolicyName = "per-client";

    public static IServiceCollection AddLibraryRateLimiting(
        this IServiceCollection services, ObservabilitySettings settings)
    {
        if (!settings.EnableRateLimiting)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PolicyName, httpContext =>
            {
                var key = ClientKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.RateLimitPermitPerWindow,
                    Window = TimeSpan.FromSeconds(settings.RateLimitWindowSeconds),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var correlationId = http.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id)
                    ? id?.ToString()
                    : null;

                http.Response.ContentType = "application/problem+json";
                await http.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too Many Requests",
                    status = 429,
                    detail = $"Client '{ClientKey(http)}' exceeded {settings.RateLimitPermitPerWindow} requests per {settings.RateLimitWindowSeconds}s.",
                    success = false,
                    errors = new[]
                    {
                        new ApiError("RATE_LIMITED", "Too many requests. Slow down and try again shortly."),
                    },
                    correlationId,
                }, cancellationToken);
            };
        });

        return services;
    }

    private static string ClientKey(HttpContext context)
    {
        var forwarded = context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff)
            ? xff.ToString().Split(',')[0].Trim()
            : null;

        return !string.IsNullOrEmpty(forwarded)
            ? forwarded
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
