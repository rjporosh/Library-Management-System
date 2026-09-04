using System.Text.RegularExpressions;
using Library.Application.Common.Errors;

namespace Library.Application.Common.Validation;

/// <summary>
/// Reusable primitive field checks shared by the CRUD validators and the
/// bulk-import pipeline. Each method appends an <see cref="ApiError"/> to
/// <paramref name="errors"/> when the value is invalid and returns whether
/// the value passed, so callers can compose without exceptions.
/// </summary>
public static partial class FieldRules
{
    [GeneratedRegex(@"^[0-9Xx\-\s]+$")] private static partial Regex IsbnCharsRegex();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")] private static partial Regex EmailRegex();

    public static bool Required(
        string? value,
        string field,
        string errorCode,
        string message,
        List<ApiError> errors,
        int? line = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        errors.Add(new ApiError(errorCode, message, field, line, Required: true));
        return false;
    }

    public static bool Isbn(string? value, string field, List<ApiError> errors, int? line = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiError(
                ErrorCodes.BookIsbnRequired, "ISBN is required.", field, line, true, SupportedValues.Isbn));
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (!IsbnCharsRegex().IsMatch(value) || (digits.Length != 10 && digits.Length != 13))
        {
            errors.Add(new ApiError(
                ErrorCodes.BookIsbnInvalid, "ISBN must be a valid 10- or 13-digit number.",
                field, line, true, SupportedValues.Isbn));
            return false;
        }

        return true;
    }

    public static bool PublishedYear(int value, string field, List<ApiError> errors, int? line = null)
    {
        var max = DateTime.UtcNow.Year + 1;
        if (value is >= 1000 && value <= max)
        {
            return true;
        }

        errors.Add(new ApiError(
            ErrorCodes.BookYearInvalid, $"Published year must be between 1000 and {max}.",
            field, line, true, SupportedValues.PublishedYear));
        return false;
    }

    public static bool Email(string? value, string field, List<ApiError> errors, int? line = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApiError(
                ErrorCodes.MemberEmailRequired, "Email is required.", field, line, true, SupportedValues.Email));
            return false;
        }

        if (!EmailRegex().IsMatch(value.Trim()))
        {
            errors.Add(new ApiError(
                ErrorCodes.MemberEmailInvalid, "Email address is not valid.", field, line, true, SupportedValues.Email));
            return false;
        }

        return true;
    }

    public static bool Enum<TEnum>(string? value, string field, string errorCode, List<ApiError> errors, int? line = null)
        where TEnum : struct, System.Enum
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            System.Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out _))
        {
            return true;
        }

        errors.Add(new ApiError(
            errorCode, $"'{value}' is not a supported value for {field}.",
            field, line, true, string.Join(", ", System.Enum.GetNames<TEnum>())));
        return false;
    }
}
