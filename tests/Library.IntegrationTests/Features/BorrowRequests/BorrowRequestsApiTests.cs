using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Features.BookCopies.Models;
using Library.Application.Features.Books.Models;
using Library.Application.Features.BorrowRequests.Models;
using Library.Application.Features.Members.Models;
using Library.Domain.Enums;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.BorrowRequests;

public sealed class BorrowRequestsApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static async Task<(Guid MemberId, Guid BookId, Guid CopyId)> SeedMemberBookAndCopyAsync(HttpClient librarian)
    {
        var member = (await (await librarian.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = $"MEM-{Guid.NewGuid():N}"[..12],
            name = "Request Tester",
            email = $"requester-{Guid.NewGuid():N}@example.com",
            phone = "+1-555-0100",
            address = "1 Test Street",
        })).Content.ReadFromJsonAsync<MemberResponse>(JsonOptions))!;

        var book = (await (await librarian.PostAsJsonAsync("/api/books", new CreateBookRequest(
            $"978000000{Random.Shared.Next(1000, 9999)}", "Requestable Book", "Test Author", 2026, "Testing", "CI Press")))
            .Content.ReadFromJsonAsync<BookResponse>(JsonOptions))!;

        var copy = (await (await librarian.PostAsJsonAsync("/api/book-copies", new CreateBookCopyRequest(
            book.Id, $"BC-REQ-{Guid.NewGuid():N}"[..14])))
            .Content.ReadFromJsonAsync<BookCopyResponse>(JsonOptions))!;

        return (member.Id, book.Id, copy.Id);
    }

    [Fact]
    public async Task Approve_BorrowRequest_IssuesTheBookAndMarksFulfilled()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var (memberId, bookId, _) = await SeedMemberBookAndCopyAsync(librarian);
        using var member = factory.CreateMemberClient(memberId);

        var createResponse = await member.PostAsJsonAsync("/api/borrow-requests",
            new CreateBorrowRequestRequest(BorrowRequestType.Borrow, bookId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;
        Assert.Equal(BorrowRequestStatus.Pending, created.Status);

        var approveResponse = await librarian.PostAsync($"/api/borrow-requests/{created.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = (await approveResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;

        Assert.Equal(BorrowRequestStatus.Fulfilled, approved.Status);
        Assert.NotNull(approved.BorrowRecordId);
    }

    [Fact]
    public async Task Create_DuplicatePendingBorrowRequest_IsRejected()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var (memberId, bookId, _) = await SeedMemberBookAndCopyAsync(librarian);
        using var member = factory.CreateMemberClient(memberId);

        var first = await member.PostAsJsonAsync("/api/borrow-requests", new CreateBorrowRequestRequest(BorrowRequestType.Borrow, bookId));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await member.PostAsJsonAsync("/api/borrow-requests", new CreateBorrowRequestRequest(BorrowRequestType.Borrow, bookId));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_PurchaseSuggestion_ThenApprove_MarksApprovedWithoutIssuing()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var (memberId, _, _) = await SeedMemberBookAndCopyAsync(librarian);
        using var member = factory.CreateMemberClient(memberId);

        var createResponse = await member.PostAsJsonAsync("/api/borrow-requests",
            new CreateBorrowRequestRequest(BorrowRequestType.Purchase, SuggestedTitle: "A New Title", SuggestedAuthor: "Some Author"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;

        var approveResponse = await librarian.PostAsync($"/api/borrow-requests/{created.Id}/approve", null);
        var approved = (await approveResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;

        Assert.Equal(BorrowRequestStatus.Approved, approved.Status);
        Assert.Null(approved.BorrowRecordId);
    }

    [Fact]
    public async Task Reject_PendingRequest_MarksRejected()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var (memberId, bookId, _) = await SeedMemberBookAndCopyAsync(librarian);
        using var member = factory.CreateMemberClient(memberId);

        var createResponse = await member.PostAsJsonAsync("/api/borrow-requests", new CreateBorrowRequestRequest(BorrowRequestType.Borrow, bookId));
        var created = (await createResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;

        var rejectResponse = await librarian.PostAsync($"/api/borrow-requests/{created.Id}/reject", null);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejected = (await rejectResponse.Content.ReadFromJsonAsync<BorrowRequestResponse>(JsonOptions))!;

        Assert.Equal(BorrowRequestStatus.Rejected, rejected.Status);
    }

    [Fact]
    public async Task Create_AsLibrarian_IsForbidden()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var response = await librarian.PostAsJsonAsync("/api/borrow-requests",
            new CreateBorrowRequestRequest(BorrowRequestType.Purchase, SuggestedTitle: "Anything"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Mine_ReturnsOnlyTheSignedInMembersRequests()
    {
        await using var factory = new LibraryApiFactory();
        using var librarian = factory.CreateLibrarianClient();

        var (memberId, bookId, _) = await SeedMemberBookAndCopyAsync(librarian);
        using var member = factory.CreateMemberClient(memberId);

        await member.PostAsJsonAsync("/api/borrow-requests", new CreateBorrowRequestRequest(BorrowRequestType.Borrow, bookId));

        var mineResponse = await member.GetAsync("/api/borrow-requests/mine");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var mine = await mineResponse.Content.ReadFromJsonAsync<List<BorrowRequestResponse>>(JsonOptions);

        Assert.NotNull(mine);
        Assert.All(mine!, r => Assert.Equal(memberId, r.MemberId));

        // Regression: the member's own list must show the book title, not blank.
        var found = Assert.Single(mine!, r => r.BookId == bookId);
        Assert.Equal("Requestable Book", found.BookTitle);
    }
}
