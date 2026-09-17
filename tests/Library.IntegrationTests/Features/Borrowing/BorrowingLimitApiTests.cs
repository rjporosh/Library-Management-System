using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Features.BookCopies.Models;
using Library.Application.Features.Books.Models;
using Library.Application.Features.Borrowing.Models;
using Library.Application.Features.Members.Models;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Borrowing;

public sealed class BorrowingLimitApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Issue_AllowsTwoActiveBorrows_ThenBlocksAThird()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var member = (await (await client.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = $"MEM-{Guid.NewGuid():N}"[..12],
            name = "Borrow Limit Tester",
            email = $"limit-{Guid.NewGuid():N}@example.com",
            phone = "+1-555-0100",
            address = "1 Test Street",
        })).Content.ReadFromJsonAsync<MemberResponse>(JsonOptions))!;

        var copyIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var book = (await (await client.PostAsJsonAsync("/api/books", new CreateBookRequest(
                $"97800000001{i:D2}", $"Borrow Limit Book {i}", "Test Author", 2026, "Testing", "CI Press")))
                .Content.ReadFromJsonAsync<BookResponse>(JsonOptions))!;

            var copy = (await (await client.PostAsJsonAsync("/api/book-copies", new CreateBookCopyRequest(
                book.Id, $"BC-LIMIT-{Guid.NewGuid():N}"[..14])))
                .Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions))!;

            copyIds.Add(copy.Id);
        }

        var first = await client.PostAsJsonAsync("/api/borrowing/issue",
            new IssueBookRequest(member.Id, copyIds[0], DateTime.UtcNow.AddDays(14)));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/borrowing/issue",
            new IssueBookRequest(member.Id, copyIds[1], DateTime.UtcNow.AddDays(14)));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var third = await client.PostAsJsonAsync("/api/borrowing/issue",
            new IssueBookRequest(member.Id, copyIds[2], DateTime.UtcNow.AddDays(14)));
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }
}
