using Library.Application.Common.Errors;

namespace Library.Application.Features.BulkImport;

/// <summary>
/// The all-or-nothing import pipeline (spec §8): validate the file, then
/// every row, detect in-file and database duplicates, and only if there are
/// zero errors persist the whole batch. Any error at any stage means nothing
/// is written and every detectable error is returned together, each carrying
/// its exact Excel row, field, code, message and accepted values.
/// </summary>
public sealed class BulkImportPipeline(IWorkbookReader workbookReader, BulkImportOptions options)
{
    private static readonly char[] FormulaTriggers = ['=', '+', '@', '\t', '\r'];

    public async Task<BulkImportOutcome> RunAsync<TEntity>(
        Stream? fileStream,
        long fileLength,
        IImportDefinition<TEntity> definition,
        CancellationToken cancellationToken)
    {
        if (fileStream is null || fileLength <= 0)
        {
            return Fail(new ApiError(ErrorCodes.ImportFileRequired, "An .xlsx file is required.", "file", Required: true));
        }

        if (fileLength > options.MaxFileSizeBytes)
        {
            return Fail(new ApiError(ErrorCodes.ImportFileTooLarge,
                $"The file exceeds the {options.MaxFileSizeBytes / 1024 / 1024} MB limit.", "file"));
        }

        WorkbookData workbook;
        try
        {
            workbook = workbookReader.Read(fileStream);
        }
        catch (WorkbookReadException ex)
        {
            return Fail(new ApiError(ErrorCodes.ImportFileUnreadable,
                $"The file could not be read as an .xlsx workbook: {ex.Message}", "file", null, null, SupportedValues.Xlsx));
        }

        // Header validation short-circuits (spec §8.1).
        var headerSet = new HashSet<string>(workbook.Headers, StringComparer.OrdinalIgnoreCase);
        var missing = definition.RequiredHeaders.Where(h => !headerSet.Contains(h)).ToList();
        if (missing.Count > 0)
        {
            return Fail(new ApiError(ErrorCodes.ImportHeaderInvalid,
                $"Missing required column(s): {string.Join(", ", missing)}.", "header", null, null,
                string.Join(", ", definition.RequiredHeaders)));
        }

        if (workbook.Rows.Count == 0)
        {
            return Fail(new ApiError(ErrorCodes.ImportNoRows, "The file contains no data rows.", "file"));
        }

        if (workbook.Rows.Count > options.MaxRows)
        {
            return Fail(new ApiError(ErrorCodes.ImportTooManyRows,
                $"The file has {workbook.Rows.Count} rows; the limit is {options.MaxRows}.", "file"));
        }

        var errors = new List<ApiError>();
        var parsed = new List<(int Row, TEntity Entity, string? Key)>();
        var keyToRows = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in workbook.Rows)
        {
            var sanitised = Sanitise(row, errors);
            if (sanitised is null)
            {
                continue;
            }

            var result = await definition.ParseRowAsync(sanitised, cancellationToken);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
                continue;
            }

            parsed.Add((row.RowNumber, result.Entity!, result.DuplicateKey));

            if (result.DuplicateKey is { } key)
            {
                if (!keyToRows.TryGetValue(key, out var list))
                {
                    list = keyToRows[key] = [];
                }

                list.Add(row.RowNumber);
            }
        }

        // In-file duplicates.
        foreach (var (key, rows) in keyToRows.Where(kv => kv.Value.Count > 1))
        {
            foreach (var rowNumber in rows)
            {
                errors.Add(new ApiError(ErrorCodes.ImportDuplicateInFile,
                    $"Duplicate value '{key}' also appears on row(s) {string.Join(", ", rows.Where(r => r != rowNumber))}.",
                    "duplicate", rowNumber));
            }
        }

        // Database conflicts.
        if (keyToRows.Count > 0)
        {
            var existing = await definition.FindExistingKeysAsync(keyToRows.Keys, cancellationToken);
            foreach (var key in existing)
            {
                foreach (var rowNumber in keyToRows[key])
                {
                    errors.Add(new ApiError(ErrorCodes.ImportDuplicateInDatabase,
                        $"A record with '{key}' already exists in the library.", "duplicate", rowNumber));
                }
            }
        }

        if (errors.Count > 0)
        {
            var truncated = errors.Count > options.MaxErrors;
            var ordered = errors
                .OrderBy(e => e.Line ?? int.MaxValue)
                .ThenBy(e => e.Field)
                .Take(options.MaxErrors)
                .ToList();

            return new BulkImportOutcome(0, ordered, truncated);
        }

        try
        {
            await definition.PersistAsync([.. parsed.Select(p => p.Entity)], cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Fail(new ApiError(ErrorCodes.ImportPersistFailed,
                "The import passed validation but could not be saved; no rows were written.", "file", null, null, ex.Message));
        }

        return BulkImportOutcome.Success(parsed.Count);
    }

    private static ImportRow? Sanitise(ImportRow row, List<ApiError> errors)
    {
        var clean = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rejected = false;

        foreach (var (column, value) in row.Cells)
        {
            if (value is null)
            {
                clean[column] = null;
                continue;
            }

            var trimmed = value.Trim();

            if (trimmed.Length > 0 && FormulaTriggers.Contains(trimmed[0]))
            {
                errors.Add(new ApiError(ErrorCodes.ImportCellFormulaRejected,
                    $"Cell '{column}' starts with '{trimmed[0]}', which is not allowed (formula-injection guard).",
                    column, row.RowNumber));
                rejected = true;
                continue;
            }

            // Collapse internal whitespace runs.
            clean[column] = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ");
        }

        return rejected ? null : new ImportRow(row.RowNumber, clean);
    }

    private BulkImportOutcome Fail(ApiError error) => new(0, [error], false);
}
