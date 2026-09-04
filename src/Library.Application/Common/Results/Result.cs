using Library.Application.Common.Errors;

namespace Library.Application.Common.Results;

/// <summary>
/// Outcome of an application operation. Carries either success or a
/// non-empty list of <see cref="ApiError"/> so callers (and the API layer)
/// can return every detectable problem in a single response rather than
/// failing on the first one (see docs/MASTER_SPECIFICATION.md §6.3).
/// </summary>
public class Result
{
    private static readonly IReadOnlyList<ApiError> NoErrors = [];

    protected Result(bool isSuccess, IReadOnlyList<ApiError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<ApiError> Errors { get; }

    /// <summary>The first error's code, when the result failed. Used to derive an HTTP status.</summary>
    public string? PrimaryErrorCode => Errors.Count > 0 ? Errors[0].ErrorCode : null;

    public static Result Success() => new(true, NoErrors);

    public static Result Failure(params ApiError[] errors) =>
        new(false, Guard(errors));

    public static Result Failure(IEnumerable<ApiError> errors) =>
        new(false, Guard([.. errors]));

    public static Result<T> Success<T>(T value) => new(value, true, NoErrors);

    public static Result<T> Failure<T>(params ApiError[] errors) =>
        new(default, false, Guard(errors));

    public static Result<T> Failure<T>(IEnumerable<ApiError> errors) =>
        new(default, false, Guard([.. errors]));

    private static IReadOnlyList<ApiError> Guard(IReadOnlyList<ApiError> errors) =>
        errors.Count > 0
            ? errors
            : throw new ArgumentException("A failed result requires at least one error.", nameof(errors));
}

/// <summary>A <see cref="Result"/> that also carries a value on success.</summary>
public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, IReadOnlyList<ApiError> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    /// <summary>The produced value. Only meaningful when <see cref="Result.IsSuccess"/> is true.</summary>
    public T? Value { get; }
}
