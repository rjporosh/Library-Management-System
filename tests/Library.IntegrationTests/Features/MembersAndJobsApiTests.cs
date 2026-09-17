using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features;

public sealed class MembersAndJobsApiTests(LibraryApiFactory factory) : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client = factory.CreateLibrarianClient();

    private async Task<JsonElement> CreateMemberAsync(string number)
    {
        var response = await _client.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = number,
            name = "Test Person",
            email = $"{number.ToLowerInvariant()}@example.com",
            phone = "+1-202-555-0000",
            address = "1 Test Street",
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Member_LifecycleEndpoints_TransitionStatus()
    {
        var member = await CreateMemberAsync($"MEM-LC-{Guid.NewGuid():N}"[..14]);
        var id = member.GetProperty("id").GetString();

        var suspended = await _client.PostAsync($"/api/members/{id}/suspend", null);
        var suspendedBody = await suspended.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Suspended", suspendedBody.GetProperty("status").GetString());

        var renewed = await _client.PostAsync($"/api/members/{id}/renew", null);
        var renewedBody = await renewed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active", renewedBody.GetProperty("status").GetString());
        Assert.False(renewedBody.GetProperty("lastRenewedAt").ValueKind == JsonValueKind.Null);

        var inactive = await _client.PostAsync($"/api/members/{id}/deactivate", null);
        var inactiveBody = await inactive.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Inactive", inactiveBody.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Member_DuplicateNumberOrEmail_IsRejectedWithEveryError()
    {
        var number = $"MEM-DUP-{Guid.NewGuid():N}"[..14];
        var first = await CreateMemberAsync(number);
        var email = first.GetProperty("email").GetString();

        var response = await _client.PostAsJsonAsync("/api/members", new
        {
            membershipNumber = number, name = "Clone", email, phone = "x", address = "y",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var codes = body.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("errorCode").GetString())
            .ToList();
        Assert.Contains("MEMBER_NUMBER_DUPLICATE", codes);
        Assert.Contains("MEMBER_EMAIL_DUPLICATE", codes);
    }

    [Fact]
    public async Task JobsEndpoint_RunsMembershipMaintenance()
    {
        var response = await _client.PostAsync("/api/jobs/member-maintenance/run", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("overdueSuspended", out _));
        Assert.True(body.TryGetProperty("expiredDeactivated", out _));
    }

    [Fact]
    public async Task Health_ReportsHealthyForInMemoryProvider()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeleteBook_WithCopies_Requires_Force_Confirmation()
    {
        // Seeded "Clean Code" has copies BC-0001 / BC-0002 (both Available).
        var books = await _client.GetFromJsonAsync<JsonElement>("/api/books");
        var cleanCode = books.GetProperty("items").EnumerateArray()
            .First(b => b.GetProperty("title").GetString() == "Clean Code");
        var id = cleanCode.GetProperty("id").GetString();

        var noForce = await _client.DeleteAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.Conflict, noForce.StatusCode);
        var body = await noForce.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BOOK_HAS_DEPENDENT_COPIES",
            body.GetProperty("errors")[0].GetProperty("errorCode").GetString());

        var forced = await _client.DeleteAsync($"/api/books/{id}?force=true");
        Assert.Equal(HttpStatusCode.NoContent, forced.StatusCode);

        var gone = await _client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
