using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library.IntegrationTests.Common;

/// <summary>
/// Shared JSON options for the integration-test HTTP client. The API serializes
/// enums as their string name (e.g. <c>"Available"</c>) via a global
/// <see cref="JsonStringEnumConverter"/>; the test client must deserialize the
/// same way, otherwise <see cref="HttpContentJsonExtensions"/> throws on any
/// response that contains an enum.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

    /// <summary>
    /// Reads the response body as <typeparamref name="T"/> using the enum-aware
    /// <see cref="Options"/>.
    /// </summary>
    public static Task<T?> ReadModelAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default) =>
        response.Content.ReadFromJsonAsync<T>(Options, cancellationToken);
}
