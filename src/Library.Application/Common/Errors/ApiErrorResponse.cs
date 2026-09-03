namespace Library.Application.Common.Errors;

/// <summary>
/// Standard error envelope every failed API response should use
/// (see docs/ROADMAP.md, Phase 3.2). Kept in Application so
/// controllers can also return it explicitly for validation errors
/// without depending on the Api-layer middleware.
/// </summary>
public sealed record ApiErrorResponse(
    bool Success,
    IReadOnlyList<ApiError> Errors,
    string? CorrelationId = null)
{
    public static ApiErrorResponse Single(
        ApiError error,
        string? correlationId = null) =>
        new(false, [error], correlationId);
}

/// <summary>One individual field/row-level error.</summary>
public sealed record ApiError(
    string ErrorCode,
    string ErrorMessage,
    string? Field = null,
    int? Line = null,
    bool? Required = null,
    string? SupportedValues = null);
