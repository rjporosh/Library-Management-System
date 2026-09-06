using Library.Application.Common.Errors;

namespace Library.Application.Common.Validation;

/// <summary>Field-level candidate values for a book, from an API request or an Excel row.</summary>
public readonly record struct BookCandidate(
    string? Isbn, string? Title, string? Author, int PublishedYear,
    string? Category, string? Publisher, string? Description);

/// <summary>Field-level candidate values for a member.</summary>
public readonly record struct MemberCandidate(
    string? MembershipNumber, string? Name, string? Email, string? Phone, string? Address);

/// <summary>Field-level candidate values for a book copy.</summary>
public readonly record struct BookCopyCandidate(string? BookReference, string? Barcode);

public static class BookValidator
{
    public static IReadOnlyList<ApiError> Validate(in BookCandidate c, int? line = null)
    {
        var errors = new List<ApiError>();
        FieldRules.Isbn(c.Isbn, "isbn", errors, line);
        FieldRules.Required(c.Title, "title", ErrorCodes.BookTitleRequired, "Title is required.", errors, line);
        FieldRules.Required(c.Author, "author", ErrorCodes.BookAuthorRequired, "Author is required.", errors, line);
        FieldRules.Required(c.Category, "category", ErrorCodes.BookCategoryRequired, "Category is required.", errors, line);
        FieldRules.Required(c.Publisher, "publisher", ErrorCodes.BookPublisherRequired, "Publisher is required.", errors, line);
        FieldRules.PublishedYear(c.PublishedYear, "publishedYear", errors, line);
        return errors;
    }
}

public static class MemberValidator
{
    public static IReadOnlyList<ApiError> Validate(in MemberCandidate c, int? line = null)
    {
        var errors = new List<ApiError>();
        FieldRules.Required(c.MembershipNumber, "membershipNumber", ErrorCodes.MemberNumberRequired, "Membership number is required.", errors, line);
        FieldRules.Required(c.Name, "name", ErrorCodes.MemberNameRequired, "Name is required.", errors, line);
        FieldRules.Email(c.Email, "email", errors, line);
        FieldRules.Required(c.Phone, "phone", ErrorCodes.MemberPhoneRequired, "Phone is required.", errors, line);
        FieldRules.Required(c.Address, "address", ErrorCodes.MemberAddressRequired, "Address is required.", errors, line);
        return errors;
    }
}

public static class BookCopyValidator
{
    public static IReadOnlyList<ApiError> Validate(in BookCopyCandidate c, int? line = null)
    {
        var errors = new List<ApiError>();
        FieldRules.Required(c.BookReference, "book", ErrorCodes.BookCopyBookRequired, "A book (ISBN or id) is required.", errors, line);
        FieldRules.Required(c.Barcode, "barcode", ErrorCodes.BookCopyBarcodeRequired, "Barcode is required.", errors, line);
        return errors;
    }
}
