using Library.Application.Features.BulkImport.Definitions;
using Library.Domain.Entities;

namespace Library.Application.Features.BulkImport;

/// <summary>Entry point the API calls for every bulk import and template download.</summary>
public sealed class BulkImportService(
    BulkImportPipeline pipeline,
    IImportTemplateWriter templateWriter,
    BookImportDefinition bookDefinition,
    MemberImportDefinition memberDefinition,
    BookCopyImportDefinition bookCopyDefinition)
{
    public Task<BulkImportOutcome> ImportBooksAsync(Stream file, long length, CancellationToken ct) =>
        pipeline.RunAsync(file, length, bookDefinition, ct);

    public Task<BulkImportOutcome> ImportMembersAsync(Stream file, long length, CancellationToken ct) =>
        pipeline.RunAsync(file, length, memberDefinition, ct);

    public Task<BulkImportOutcome> ImportBookCopiesAsync(Stream file, long length, CancellationToken ct) =>
        pipeline.RunAsync(file, length, bookCopyDefinition, ct);

    public (byte[] Content, string FileName) BookTemplate() => Template(bookDefinition.Template);

    public (byte[] Content, string FileName) MemberTemplate() => Template(memberDefinition.Template);

    public (byte[] Content, string FileName) BookCopyTemplate() => Template(bookCopyDefinition.Template);

    private (byte[], string) Template(ImportTemplateSpec spec) =>
        (templateWriter.Build(spec), $"{spec.ResourceName}.xlsx");
}
