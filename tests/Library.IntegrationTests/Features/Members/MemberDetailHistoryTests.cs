using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Features.BookCopies.Models;
using Library.Application.Features.Books.Models;
using Library.Application.Features.Borrowing.Models;
using Library.Application.Features.Members.Models;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Members;

public sealed class MemberDetailHistoryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetDetail_HistoryEntries_IncludeBarcodeAndBookTitle()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var member = (await (await client.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = $"MEM-{Guid.NewGuid():N}"[..12],
            name = "History Tester",
            email = $"history-{Guid.NewGuid():N}@example.com",
            phone = "+1-555-0100",
            address = "1 Test Street",
        })).Content.ReadFromJsonAsync<MemberResponse>(JsonOptions))!;

        var book = (await (await client.PostAsJsonAsync("/api/books", new CreateBookRequest(
            "9780000000299", "History Test Book", "Test Author", 2026, "Testing", "CI Press")))
            .Content.ReadFromJsonAsync<BookResponse>(JsonOptions))!;

        var copy = (await (await client.PostAsJsonAsync("/api/book-copies", new CreateBookCopyRequest(
            book.Id, $"BC-HIST-{Guid.NewGuid():N}"[..14])))
            .Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions))!;

        await client.PostAsJsonAsync("/api/borrowing/issue",
            new IssueBookRequest(member.Id, copy.Id, DateTime.UtcNow.AddDays(14)));

        var detailResponse = await client.GetAsync($"/api/members/{member.Id}/detail");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var detail = await detailResponse.Content.ReadFromJsonAsync<MemberDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        var entry = Assert.Single(detail!.History);

        Assert.Equal(copy.Barcode, entry.Barcode);
        Assert.Equal("History Test Book", entry.BookTitle);
    }
}
