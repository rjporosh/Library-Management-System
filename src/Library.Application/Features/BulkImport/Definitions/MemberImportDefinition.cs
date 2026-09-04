using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Validation;
using Library.Domain.Entities;

namespace Library.Application.Features.BulkImport.Definitions;

public sealed class MemberImportDefinition(IMemberRepository memberRepository) : IImportDefinition<Member>
{
    public string ResourceName => "member";

    public IReadOnlyList<string> RequiredHeaders { get; } = ["MembershipNumber", "Name", "Email"];

    public ImportTemplateSpec Template { get; } = new(
        "member-import-template",
        "Members",
        [
            new ImportColumn("MembershipNumber", true, "Unique membership number/card id."),
            new ImportColumn("Name", true, "Member full name."),
            new ImportColumn("Email", true, "Contact email address.", SupportedValues.Email)
        ],
        [
            ["MEM-100010", "Grace Hopper", "grace@example.com"],
            ["MEM-100011", "Alan Turing", "alan@example.com"]
        ]);

    public ValueTask<RowParseResult<Member>> ParseRowAsync(ImportRow row, CancellationToken cancellationToken) =>
        ValueTask.FromResult(ParseRow(row));

    private static RowParseResult<Member> ParseRow(ImportRow row)
    {
        var number = row.Cells.GetValueOrDefault("MembershipNumber");
        var name = row.Cells.GetValueOrDefault("Name");
        var email = row.Cells.GetValueOrDefault("Email");

        var errors = MemberValidator.Validate(new MemberCandidate(number, name, email), row.RowNumber);
        if (errors.Count > 0)
        {
            return new RowParseResult<Member>(null, errors, null);
        }

        var member = new Member(Guid.NewGuid(), number!.Trim(), name!.Trim(), email!.Trim());
        return new RowParseResult<Member>(member, [], number!.Trim().ToLowerInvariant());
    }

    public async Task<IReadOnlySet<string>> FindExistingKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (await memberRepository.ExistsByMembershipNumberAsync(key, null, cancellationToken))
            {
                existing.Add(key);
            }
        }

        return existing;
    }

    public Task PersistAsync(IReadOnlyList<Member> entities, CancellationToken cancellationToken) =>
        memberRepository.AddRangeAsync(entities, cancellationToken);
}
