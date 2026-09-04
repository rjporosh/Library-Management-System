using Library.Application.Common.Errors;

namespace Library.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request fails one or more validation rules. Carries the
/// full list of errors so the API layer can return them together
/// (docs/MASTER_SPECIFICATION.md §6.3 - "never stop after the first
/// validation error").
/// </summary>
public sealed class ValidationException(IReadOnlyList<ApiError> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyList<ApiError> Errors { get; } = errors;
}
