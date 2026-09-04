using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Validation;
using Library.Domain.Entities;

namespace Library.Application.Features.BulkImport.Definitions;

public sealed class BookImportDefinition(IBookRepository bookRepository) : IImportDefinition<Book>
{
    public string ResourceName => "book";

    public IReadOnlyList<string> RequiredHeaders { get; } = ["ISBN", "Title", "Author", "PublishedYear"];

    public ImportTemplateSpec Template { get; } = new(
        "book-import-template",
        "Books",
        [
            new ImportColumn("ISBN", true, "10- or 13-digit ISBN; hyphens allowed.", SupportedValues.Isbn),
            new ImportColumn("Title", true, "Book title."),
            new ImportColumn("Author", true, "Primary author."),
            new ImportColumn("PublishedYear", true, "Year of publication.", SupportedValues.PublishedYear),
            new ImportColumn("Description", false, "Optional short description.")
        ],
        [
            ["9780132350884", "Clean Code", "Robert C. Martin", "2008", "A handbook of agile software craftsmanship."],
            ["978-0135957059", "The Pragmatic Programmer", "David Thomas & Andrew Hunt", "2019", "Your journey to mastery."]
        ]);

    public ValueTask<RowParseResult<Book>> ParseRowAsync(ImportRow row, CancellationToken cancellationToken) =>
        ValueTask.FromResult(ParseRow(row));

    private static RowParseResult<Book> ParseRow(ImportRow row)
    {
        var isbn = row.Cells.GetValueOrDefault("ISBN");
        var title = row.Cells.GetValueOrDefault("Title");
        var author = row.Cells.GetValueOrDefault("Author");
        var yearRaw = row.Cells.GetValueOrDefault("PublishedYear");
        var description = row.Cells.GetValueOrDefault("Description");

        var errors = new List<ApiError>();
        var year = 0;

        if (string.IsNullOrWhiteSpace(yearRaw) || !int.TryParse(yearRaw, out year))
        {
            errors.Add(new ApiError(ErrorCodes.BookYearInvalid, "Published year must be a whole number.",
                "publishedYear", row.RowNumber, true, SupportedValues.PublishedYear));
        }

        errors.AddRange(BookValidator.Validate(new BookCandidate(isbn, title, author, year, description), row.RowNumber));

        if (errors.Count > 0)
        {
            return new RowParseResult<Book>(null, errors, null);
        }

        var book = new Book(Guid.NewGuid(), isbn!.Trim(), title!.Trim(), author!.Trim(), year,
            string.IsNullOrWhiteSpace(description) ? null : description!.Trim());

        return new RowParseResult<Book>(book, [], NormaliseIsbn(isbn!));
    }

    public async Task<IReadOnlySet<string>> FindExistingKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (await bookRepository.ExistsByIsbnAsync(key, null, cancellationToken))
            {
                existing.Add(key);
            }
        }

        return existing;
    }

    public Task PersistAsync(IReadOnlyList<Book> entities, CancellationToken cancellationToken) =>
        bookRepository.AddRangeAsync(entities, cancellationToken);

    private static string NormaliseIsbn(string isbn) => new(isbn.Where(char.IsLetterOrDigit).ToArray());
}
