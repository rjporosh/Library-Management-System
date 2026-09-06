using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features;

public sealed class LocalizationAndProblemDetailsTests(LibraryApiFactory factory)
    : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Messages_DefaultCulture_IsEnglish()
    {
        var doc = await _client.GetFromJsonAsync<JsonElement>("/api/metadata/messages");

        Assert.Equal("en", doc.GetProperty("culture").GetString());
        Assert.Equal("ISBN is required.",
            doc.GetProperty("messages").GetProperty("BOOK_ISBN_REQUIRED").GetString());
    }

    [Fact]
    public async Task Messages_BanglaCulture_ReturnsBanglaText()
    {
        var doc = await _client.GetFromJsonAsync<JsonElement>("/api/metadata/messages?culture=bn");

        Assert.Equal("bn", doc.GetProperty("culture").GetString());
        var isbn = doc.GetProperty("messages").GetProperty("BOOK_ISBN_REQUIRED").GetString();
        Assert.NotEqual("ISBN is required.", isbn);
        Assert.Contains("ISBN", isbn); // "ISBN আবশ্যক।"
    }

    [Fact]
    public async Task Messages_AcceptLanguageHeader_IsHonoured()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metadata/messages");
        request.Headers.Add("Accept-Language", "bn");

        var response = await request.SendVia(_client);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("bn", doc.GetProperty("culture").GetString());
    }

    [Fact]
    public async Task Validation_Failure_ReturnsRfc7807ProblemJson_WithEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            isbn = "", title = "", author = "A", publishedYear = 1500, category = "", publisher = "",
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(422, doc.GetProperty("status").GetInt32());
        Assert.Equal("/api/books", doc.GetProperty("instance").GetString());
        Assert.False(doc.GetProperty("success").GetBoolean());
        Assert.True(doc.GetProperty("errors").GetArrayLength() >= 3);
    }
}

file static class HttpRequestExtensions
{
    public static Task<HttpResponseMessage> SendVia(this HttpRequestMessage request, HttpClient client) =>
        client.SendAsync(request);
}
