using Library.Application.Common.Errors;

namespace Library.Application.Features.BulkImport;

/// <summary>Limits for an import request. Bound from the "BulkImport" config section.</summary>
public sealed class BulkImportOptions
{
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    public int MaxRows { get; set; } = 5000;

    /// <summary>Cap on how many errors are returned in one response.</summary>
    public int MaxErrors { get; set; } = 500;
}

/// <summary>One data row read from the workbook. <see cref="RowNumber"/> is the 1-based Excel row.</summary>
public sealed record ImportRow(int RowNumber, IReadOnlyDictionary<string, string?> Cells);

/// <summary>Parsed workbook contents.</summary>
public sealed record WorkbookData(IReadOnlyList<string> Headers, IReadOnlyList<ImportRow> Rows);

/// <summary>Thrown by an <c>IWorkbookReader</c> when the stream is not a readable .xlsx.</summary>
public sealed class WorkbookReadException(string message) : Exception(message);

/// <summary>Reads an .xlsx stream into <see cref="WorkbookData"/>.</summary>
public interface IWorkbookReader
{
    WorkbookData Read(Stream stream);
}

/// <summary>One column in a downloadable import template.</summary>
public sealed record ImportColumn(string Header, bool Required, string Description, string? SupportedValues = null);

/// <summary>Everything needed to generate a template workbook.</summary>
public sealed record ImportTemplateSpec(
    string ResourceName,
    string SheetName,
    IReadOnlyList<ImportColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> ExampleRows);

/// <summary>Builds a formatted .xlsx template from an <see cref="ImportTemplateSpec"/>.</summary>
public interface IImportTemplateWriter
{
    byte[] Build(ImportTemplateSpec spec);
}

/// <summary>The result of validating and (if clean) parsing one row.</summary>
public sealed record RowParseResult<TEntity>(TEntity? Entity, IReadOnlyList<ApiError> Errors, string? DuplicateKey)
{
    public bool IsValid => Errors.Count == 0 && Entity is not null;
}

/// <summary>The outcome of a bulk-import request.</summary>
public sealed record BulkImportOutcome(int Imported, IReadOnlyList<ApiError> Errors, bool Truncated)
{
    public bool Succeeded => Errors.Count == 0;

    public static BulkImportOutcome Success(int imported) => new(imported, [], false);
}
