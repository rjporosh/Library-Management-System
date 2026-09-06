using Library.Application.Common.Errors;
using Library.Application.Features.BulkImport;
using Library.Application.Features.BulkImport.Definitions;
using Library.Domain.Entities;
using Library.UnitTests.Common;

using Library.Infrastructure.Persistence;

namespace Library.UnitTests.Features.BulkImport;

/// <summary>Covers the ten bulk-import acceptance scenarios from MASTER_SPECIFICATION.md §21.</summary>
public sealed class BookImportPipelineTests
{
    private static readonly string[] Headers =
        ["ISBN", "Title", "Author", "Category", "Publisher", "PublishedYear", "Description"];

    private static (BulkImportPipeline Pipeline, StubBookRepository Books) Build(WorkbookData workbook)
    {
        var books = new StubBookRepository(treatAllIdsAsExisting: false);
        var reader = new FakeWorkbookReader(workbook);
        var pipeline = new BulkImportPipeline(reader, new BulkImportOptions(), new NoOpUnitOfWork());
        return (pipeline, books);
    }

    private static WorkbookData Sheet(params (string Isbn, string Title, string Author, string Year, string Desc)[] rows)
    {
        var importRows = rows.Select((r, i) => new ImportRow(i + 2, new Dictionary<string, string?>
        {
            ["ISBN"] = r.Isbn,
            ["Title"] = r.Title,
            ["Author"] = r.Author,
            ["Category"] = "Software Engineering",
            ["Publisher"] = "Test Press",
            ["PublishedYear"] = r.Year,
            ["Description"] = r.Desc
        })).ToList<ImportRow>();

        return new WorkbookData(Headers, importRows);
    }

    private static Task<BulkImportOutcome> Run(BulkImportPipeline pipeline, StubBookRepository books) =>
        pipeline.RunAsync(new MemoryStream([1]), 1, new BookImportDefinition(books), CancellationToken.None);

    [Fact]
    public async Task AllValidRows_AreImported()
    {
        var (pipeline, books) = Build(Sheet(
            ("9780132350884", "Clean Code", "Robert C. Martin", "2008", "x"),
            ("9780135957059", "The Pragmatic Programmer", "Hunt & Thomas", "2019", "")));

        var outcome = await Run(pipeline, books);

        Assert.True(outcome.Succeeded);
        Assert.Equal(2, outcome.Imported);
        Assert.Equal(2, books.Books.Count);
    }

    [Fact]
    public async Task MissingRequiredField_RollsBackEverything()
    {
        var (pipeline, books) = Build(Sheet(
            ("9780132350884", "Clean Code", "Robert C. Martin", "2008", "x"),
            ("9780135957059", "", "Hunt", "2019", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Empty(books.Books);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.BookTitleRequired && e.Line == 3);
    }

    [Fact]
    public async Task InvalidIsbn_RollsBackEverything()
    {
        var (pipeline, books) = Build(Sheet(("12345", "T", "A", "2000", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Empty(books.Books);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.BookIsbnInvalid && e.Line == 2);
    }

    [Fact]
    public async Task InvalidYear_RollsBackEverything()
    {
        var (pipeline, books) = Build(Sheet(("9780132350884", "T", "A", "abcd", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Empty(books.Books);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.BookYearInvalid && e.Line == 2);
    }

    [Fact]
    public async Task DuplicateIsbnWithinFile_RollsBackEverything()
    {
        var (pipeline, books) = Build(Sheet(
            ("9780132350884", "A", "A", "2000", ""),
            ("978-0132350884", "B", "B", "2001", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Empty(books.Books);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.ImportDuplicateInFile);
    }

    [Fact]
    public async Task DuplicateIsbnAgainstDatabase_RollsBackEverything()
    {
        var (pipeline, books) = Build(Sheet(("9780132350884", "A", "A", "2000", "")));
        books.Books.Add(new Book(Guid.NewGuid(), "9780132350884", "Existing", "X", 2000));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Single(books.Books); // only the pre-existing one
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.ImportDuplicateInDatabase && e.Line == 2);
    }

    [Fact]
    public async Task MalformedHeader_IsRejected()
    {
        var workbook = new WorkbookData(["ISBN", "Title"], [new ImportRow(2, new Dictionary<string, string?> { ["ISBN"] = "9780132350884", ["Title"] = "T" })]);
        var (pipeline, books) = Build(workbook);

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.ImportHeaderInvalid);
        Assert.Contains("Author", outcome.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MultipleErrors_AreReturnedTogether_WithExactRowFieldAndSupportedValues()
    {
        var (pipeline, books) = Build(Sheet(
            ("", "T1", "A1", "2000", ""),
            ("9780132350884", "", "A2", "abcd", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.True(outcome.Errors.Count >= 3);
        Assert.Contains(outcome.Errors, e => e.Field == "isbn" && e.Line == 2 && e.Required == true);
        Assert.Contains(outcome.Errors, e => e.Field == "title" && e.Line == 3);
        Assert.Contains(outcome.Errors, e => e.Field == "publishedYear" && e.Line == 3 && e.SupportedValues == SupportedValues.PublishedYear);
    }

    [Fact]
    public async Task FormulaInjectionCell_IsRejected()
    {
        var (pipeline, books) = Build(Sheet(("9780132350884", "=SUM(A1:A2)", "A", "2000", "")));

        var outcome = await Run(pipeline, books);

        Assert.False(outcome.Succeeded);
        Assert.Empty(books.Books);
        Assert.Contains(outcome.Errors, e => e.ErrorCode == ErrorCodes.ImportCellFormulaRejected && e.Line == 2);
    }

    private sealed class FakeWorkbookReader(WorkbookData data) : IWorkbookReader
    {
        public WorkbookData Read(Stream stream) => data;
    }
}
