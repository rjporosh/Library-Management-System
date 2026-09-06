using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Validation;
using Library.Domain.Entities;

namespace Library.Application.Features.BulkImport.Definitions;

public sealed class MemberImportDefinition(IMemberRepository memberRepository) : IImportDefinition<Member>
{
    public string ResourceName => "member";

    public IReadOnlyList<string> RequiredHeaders { get; } = ["MembershipNumber", "Name", "Email", "Phone", "Address"];

    public ImportTemplateSpec Template { get; } = new(
        "member-import-template",
        "Members",
        [
            new ImportColumn("MembershipNumber", true, "Unique membership number/card id."),
            new ImportColumn("Name", true, "Member full name."),
            new ImportColumn("Email", true, "Contact email address.", SupportedValues.Email),
            new ImportColumn("Phone", true, "Contact phone number."),
            new ImportColumn("Address", true, "Postal address.")
        ],
        [
            ["MEM-100010", "Grace Hopper", "grace@example.com", "+1-202-555-0110", "1 Navy Yard, Washington DC"],
            ["MEM-100011", "Alan Turing", "alan@example.com", "+44-20-7946-0000", "Bletchley Park, Milton Keynes"]
        ]);

    public ValueTask<RowParseResult<Member>> ParseRowAsync(ImportRow row, CancellationToken cancellationToken) =>
        ValueTask.FromResult(ParseRow(row));

    private static RowParseResult<Member> ParseRow(ImportRow row)
    {
        var number = row.Cells.GetValueOrDefault("MembershipNumber");
        var name = row.Cells.GetValueOrDefault("Name");
        var email = row.Cells.GetValueOrDefault("Email");
        var phone = row.Cells.GetValueOrDefault("Phone");
        var address = row.Cells.GetValueOrDefault("Address");

        var errors = MemberValidator.Validate(
            new MemberCandidate(number, name, email, phone, address), row.RowNumber);
        if (errors.Count > 0)
        {
            return new RowParseResult<Member>(null, errors, null);
        }

        var member = new Member(Guid.NewGuid(), number!.Trim(), name!.Trim(), email!.Trim(),
            membershipExpiresAt: null, phone: phone!.Trim(), address: address!.Trim());
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
