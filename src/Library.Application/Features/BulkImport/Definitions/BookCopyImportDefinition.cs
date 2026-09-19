using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Domain.Entities;

namespace Library.Application.Features.BulkImport.Definitions;

/// <summary>
/// Book copies are imported by <b>book ISBN</b> (librarian-friendly) rather
/// than internal book id. Every referenced ISBN must already exist.
/// </summary>
public sealed class BookCopyImportDefinition(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository) : IImportDefinition<BookCopy>
{
    public string ResourceName => "book-copy";

    public IReadOnlyList<string> RequiredHeaders { get; } = ["ISBN", "Barcode"];

    public ImportTemplateSpec Template { get; } = new(
        "book-copy-import-template",
        "BookCopies",
        [
            new ImportColumn("ISBN", true, "ISBN of an existing book."),
            new ImportColumn("Barcode", true, "Unique barcode for this physical copy.")
        ],
        [
            ["9780134494166", "BC-TPL-0001"],
            ["9780134494166", "BC-TPL-0002"]
        ]);

    public async ValueTask<RowParseResult<BookCopy>> ParseRowAsync(ImportRow row, CancellationToken cancellationToken)
    {
        var isbn = row.Cells.GetValueOrDefault("ISBN");
        var barcode = row.Cells.GetValueOrDefault("Barcode");

        var errors = new List<ApiError>();
        if (string.IsNullOrWhiteSpace(isbn))
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBookRequired, "ISBN is required.", "isbn", row.RowNumber, true));
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBarcodeRequired, "Barcode is required.", "barcode", row.RowNumber, true));
        }

        if (errors.Count > 0)
        {
            return new RowParseResult<BookCopy>(null, errors, null);
        }

        var book = await bookRepository.GetByIsbnAsync(isbn!.Trim(), cancellationToken);
        if (book is null)
        {
            return new RowParseResult<BookCopy>(null,
                [new ApiError(ErrorCodes.BookCopyBookNotFound, $"No book with ISBN '{isbn}' exists.", "isbn", row.RowNumber)],
                null);
        }

        var copy = new BookCopy(Guid.NewGuid(), book.Id, barcode!.Trim());
        return new RowParseResult<BookCopy>(copy, [], barcode!.Trim().ToLowerInvariant());
    }

    public async Task<IReadOnlySet<string>> FindExistingKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (await bookCopyRepository.ExistsByBarcodeAsync(key, null, cancellationToken))
            {
                existing.Add(key);
            }
        }

        return existing;
    }

    public Task PersistAsync(IReadOnlyList<BookCopy> entities, CancellationToken cancellationToken) =>
        bookCopyRepository.AddRangeAsync(entities, cancellationToken);
}
