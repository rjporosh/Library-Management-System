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
}
