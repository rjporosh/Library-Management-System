using System.Net;
using System.Net.Http.Json;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.ReleaseNotes;

public sealed class ReleaseNotesApiTests(LibraryApiFactory factory) : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetCurrent_ReturnsPopulatedReleaseNotes()
    {
        var response = await _client.GetAsync("/api/release-notes/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var note = await response.Content.ReadFromJsonAsync<CurrentRelease>();

        Assert.NotNull(note);
        Assert.False(string.IsNullOrWhiteSpace(note.Version));
        Assert.False(string.IsNullOrWhiteSpace(note.ReleaseDate));
        Assert.NotEmpty(note.NewFeatures);
        Assert.NotEmpty(note.QaChecklist);
    }

    private sealed record CurrentRelease(
        string Version,
        string ReleaseDate,
        IReadOnlyList<string> NewFeatures,
        IReadOnlyList<string> QaChecklist);
}
