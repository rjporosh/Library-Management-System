using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Common.Pagination;
using Library.Application.Features.BookCopies.Models;
using Library.Application.Features.Books.Models;
using Library.Application.Features.Borrowing.Models;
using Library.Application.Features.Members.Models;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Members;

public sealed class MemberSearchBorrowingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Search_ReportsCurrentlyBorrowedCount()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var member = (await (await client.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = $"MEM-{Guid.NewGuid():N}"[..12],
            name = "Currently Borrowing Tester",
            email = $"borrower-{Guid.NewGuid():N}@example.com",
            phone = "+1-555-0100",
            address = "1 Test Street",
        })).Content.ReadFromJsonAsync<MemberResponse>(JsonOptions))!;

        var book = (await (await client.PostAsJsonAsync("/api/books", new CreateBookRequest(
            "9780000000199", "Currently Borrowed Book", "Test Author", 2026, "Testing", "CI Press")))
            .Content.ReadFromJsonAsync<BookResponse>(JsonOptions))!;

        var copy = (await (await client.PostAsJsonAsync("/api/book-copies", new CreateBookCopyRequest(
            book.Id, $"BC-CUR-{Guid.NewGuid():N}"[..14])))
            .Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions))!;

        await client.PostAsJsonAsync("/api/borrowing/issue",
            new IssueBookRequest(member.Id, copy.Id, DateTime.UtcNow.AddDays(14)));

        var searchResponse = await client.PostAsJsonAsync("/api/members/search", new
        {
            filters = new[] { new { field = "membershipNumber", @operator = "eq", value = member.MembershipNumber } },
        });

        var page = await searchResponse.Content.ReadFromJsonAsync<PagedResult<MemberResponse>>(JsonOptions);
        var found = Assert.Single(page!.Items);
        Assert.Equal(1, found.CurrentlyBorrowed);
    }
}
