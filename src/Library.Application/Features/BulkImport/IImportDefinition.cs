namespace Library.Application.Features.BulkImport;

/// <summary>
/// Everything entity-specific the <see cref="BulkImportPipeline"/> needs:
/// the template, the expected headers, how to validate/parse a row, how to
/// detect conflicts with existing records, and how to persist the batch.
/// </summary>
public interface IImportDefinition<TEntity>
{
    /// <summary>Used in messages and the template file name, e.g. "book".</summary>
    string ResourceName { get; }

    ImportTemplateSpec Template { get; }

    /// <summary>Column headers that must be present (case-insensitive, order-independent).</summary>
    IReadOnlyList<string> RequiredHeaders { get; }

    /// <summary>Validates and, when valid, parses one sanitised row.</summary>
    ValueTask<RowParseResult<TEntity>> ParseRowAsync(ImportRow row, CancellationToken cancellationToken);

    /// <summary>
    /// Given the duplicate keys collected from the file, returns the subset
    /// that already exists in the database.
    /// </summary>
    Task<IReadOnlySet<string>> FindExistingKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);

    /// <summary>Persists the fully-validated batch (single unit of work).</summary>
    Task PersistAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken);
}
