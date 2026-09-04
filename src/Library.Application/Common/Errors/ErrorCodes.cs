namespace Library.Application.Common.Errors;

/// <summary>
/// Stable, machine-readable error codes. Front-end and integration tests
/// key on these; the human-readable message and <c>supportedValues</c>
/// hint live next to each rule that raises it. Never rename an existing
/// code - only add.
/// </summary>
public static class ErrorCodes
{
    // Generic
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InternalError = "INTERNAL_SERVER_ERROR";
    public const string FeatureDisabled = "FEATURE_DISABLED";

    // Book
    public const string BookIsbnRequired = "BOOK_ISBN_REQUIRED";
    public const string BookIsbnInvalid = "BOOK_ISBN_INVALID";
    public const string BookTitleRequired = "BOOK_TITLE_REQUIRED";
    public const string BookAuthorRequired = "BOOK_AUTHOR_REQUIRED";
    public const string BookYearInvalid = "BOOK_YEAR_INVALID";
    public const string BookIsbnDuplicate = "BOOK_ISBN_DUPLICATE";
    public const string BookNotFound = "BOOK_NOT_FOUND";

    // Member
    public const string MemberNumberRequired = "MEMBER_NUMBER_REQUIRED";
    public const string MemberNameRequired = "MEMBER_NAME_REQUIRED";
    public const string MemberEmailRequired = "MEMBER_EMAIL_REQUIRED";
    public const string MemberEmailInvalid = "MEMBER_EMAIL_INVALID";
    public const string MemberNumberDuplicate = "MEMBER_NUMBER_DUPLICATE";
    public const string MemberEmailDuplicate = "MEMBER_EMAIL_DUPLICATE";
    public const string MemberNotFound = "MEMBER_NOT_FOUND";
    public const string MemberHasActiveBorrow = "MEMBER_HAS_ACTIVE_BORROW";
    public const string MemberStatusInvalid = "MEMBER_STATUS_INVALID";

    // Book copy
    public const string BookCopyBarcodeRequired = "BOOKCOPY_BARCODE_REQUIRED";
    public const string BookCopyBookRequired = "BOOKCOPY_BOOK_REQUIRED";
    public const string BookCopyBookNotFound = "BOOKCOPY_BOOK_NOT_FOUND";
    public const string BookCopyBarcodeDuplicate = "BOOKCOPY_BARCODE_DUPLICATE";
    public const string BookCopyNotFound = "BOOKCOPY_NOT_FOUND";
    public const string BookCopyStatusInvalid = "BOOKCOPY_STATUS_INVALID";
    public const string BookCopyBorrowed = "BOOKCOPY_BORROWED";

    // Search
    public const string SearchFieldUnknown = "SEARCH_FIELD_UNKNOWN";
    public const string SearchOperatorUnsupported = "SEARCH_OPERATOR_UNSUPPORTED";
    public const string SearchValueInvalid = "SEARCH_VALUE_INVALID";

    // Bulk import
    public const string ImportFileRequired = "IMPORT_FILE_REQUIRED";
    public const string ImportFileTooLarge = "IMPORT_FILE_TOO_LARGE";
    public const string ImportFileType = "IMPORT_FILE_TYPE_INVALID";
    public const string ImportFileUnreadable = "IMPORT_FILE_UNREADABLE";
    public const string ImportHeaderInvalid = "IMPORT_HEADER_INVALID";
    public const string ImportTooManyRows = "IMPORT_TOO_MANY_ROWS";
    public const string ImportNoRows = "IMPORT_NO_ROWS";
    public const string ImportCellFormulaRejected = "IMPORT_CELL_FORMULA_REJECTED";
    public const string ImportDuplicateInFile = "IMPORT_DUPLICATE_IN_FILE";
    public const string ImportDuplicateInDatabase = "IMPORT_DUPLICATE_IN_DATABASE";
    public const string ImportRowInvalid = "IMPORT_ROW_INVALID";
    public const string ImportPersistFailed = "IMPORT_PERSIST_FAILED";
}

/// <summary>Human-readable descriptions of the accepted value / format for a field.</summary>
public static class SupportedValues
{
    public const string Isbn = "ONLY DIGITS (hyphens allowed), 10 OR 13 DIGITS";
    public const string PublishedYear = "ONLY NUMBER, 4 DIGITS, BETWEEN 1000 AND (CURRENT YEAR + 1)";
    public const string Email = "A VALID EMAIL ADDRESS, e.g. name@example.com";
    public const string Xlsx = ".xlsx (Office Open XML) WORKBOOK";
}
