using System.Net;
using System.Net.Http.Json;
using Library.Application.Features.Assistant;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Assistant;

public sealed class AssistantApiTests
{
    [Fact]
    public async Task Chat_CopyCountQuestion_AnswersFromSeedData()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("How many copies of \"Clean Code\" are available?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(body);
        Assert.Equal("RuleBased", body!.Provider);
        Assert.Contains("Clean Code", body.Answer);
    }

    [Fact]
    public async Task Chat_CopyCountQuestion_UnquotedNaturalPhrasing_StripsTrailingFiller()
    {
        // Regression: "copies of X are available?" used to capture "X are
        // available" as the title (including the trailing filler words),
        // so the book was never found.
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("How many copies of Clean Code are available?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(body);
        Assert.Contains("Clean Code", body!.Answer);
        Assert.DoesNotContain("couldn't find", body.Answer);
    }

    [Fact]
    public async Task Chat_MostBorrowedQuestion_ReturnsAnAnswer()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("What are the most borrowed books this month?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Answer));
    }

    [Fact]
    public async Task Chat_TopBorrowersQuestion_ReturnsAnAnswer()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("Who borrowed the most books last month?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Answer));
    }

    [Fact]
    public async Task Chat_UnrecognisedQuestion_ReturnsHelpText()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new ChatRequest("What's the weather like?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains("I can answer questions", body!.Answer);
    }

    [Fact]
    public async Task Chat_EmptyMessage_ReturnsBadRequest()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new ChatRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Chat_AsMember_IsForbidden()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateMemberClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new ChatRequest("How many copies of Clean Code?"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("How many copies of Clean Code are borrowed?", "0 copies", "2 total, 2 available, 0 borrowed")]
    [InlineData("How many copies of Refactoring are borrowed?", "1 copy", "2 total, 1 available, 1 borrowed")]
    [InlineData("How many total copies of books by Martin Fowler?", "2 copies", "Refactoring")]
    [InlineData("How many books are borrowed from publisher Addison-Wesley?", "2 copies", "Across 3 matching books")]
    [InlineData("How many copies of the 20th edition are borrowed?", "1 copy", "Pragmatic Programmer")]
    public async Task Chat_BookStatsQuestion_AnswersFromSeedData(string question, string headlineFragment, string detailFragment)
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new ChatRequest(question));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains(headlineFragment, body!.Answer);
        Assert.Contains(detailFragment, body.Answer);
    }

    [Fact]
    public async Task Chat_SingleWriterBookCount_ReturnsSummary()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("How many books by writer Robert C. Martin do we have?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains("Robert C. Martin", body!.Answer);
        Assert.Contains("Clean Code", body.Answer);
    }

    [Fact]
    public async Task Chat_BorrowsTimeframe_ReturnsBorrowCounts()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("How many books are on borrow this month?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains("borrow", body!.Answer);
    }

    [Fact]
    public async Task Chat_BestBorrowedBook_ReturnsBestBook()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("Which is the best borrowed book?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains("The best borrowed book", body!.Answer);
    }

    [Fact]
    public async Task Chat_ThresholdAlarm_ReturnsBuyMoreAlarm()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("Check low stock threshold alarm (buy more books)"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Contains("ALARM", body!.Answer);
        Assert.Contains("Buy more copies", body.Answer);
    }

    [Fact]
    public async Task Chat_BengaliQuestions_ReturnsBengaliAnswersWithBengaliDigits()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        // 1. Author question in Bengali
        var res1 = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("রবার্ট সি. মার্টিনের কয়টি বই আছে?"));
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var body1 = await res1.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body1);
        Assert.Contains("রবার্ট সি. মার্টিন", body1!.Answer);
        Assert.Contains("টি", body1.Answer);

        // 2. Threshold alarm question in Bengali
        var res2 = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("কম স্টকের বই বা অ্যালার্ম দেখাও"));
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var body2 = await res2.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body2);
        Assert.Contains("অ্যালার্ম", body2!.Answer);
        Assert.Contains("ক্রয়", body2.Answer);

        // 3. Best borrowed book in Bengali
        var res3 = await client.PostAsJsonAsync("/api/assistant/chat",
            new ChatRequest("সবচেয়ে বেশি ধার নেওয়া বই কোনটি?"));
        Assert.Equal(HttpStatusCode.OK, res3.StatusCode);
        var body3 = await res3.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body3);
        Assert.Contains("সেরা", body3!.Answer);
    }
}
