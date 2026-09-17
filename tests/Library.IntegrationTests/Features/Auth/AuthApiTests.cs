using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Features.Auth.Models;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Auth;

public sealed class AuthApiTests(LibraryApiFactory factory) : IClassFixture<LibraryApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithSeededLibrarian_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("librarian", "Librarian@123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Librarian", body!.Role.ToString());
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequestWithError()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("librarian", "wrong-password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewMember_CreatesAccountAndLogsIn()
    {
        var email = $"newmember-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterMemberRequest("New Member", email, "SecurePass1", "+1-555-0100", "1 Test Street"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Member", body!.Role.ToString());
        Assert.NotNull(body.MemberId);
    }

    [Fact]
    public async Task Books_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymous = factory.CreateClient();
        var response = await anonymous.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Books_Create_WithMemberToken_ReturnsForbidden()
    {
        using var memberClient = factory.CreateMemberClient(Guid.NewGuid());
        var response = await memberClient.PostAsJsonAsync("/api/books", new
        {
            isbn = "9781234567897", title = "Some Book", author = "Some Author",
            publishedYear = 2020, category = "Fiction", publisher = "Some Publisher",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Books_Browse_WithMemberToken_Succeeds()
    {
        using var memberClient = factory.CreateMemberClient(Guid.NewGuid());
        var response = await memberClient.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
