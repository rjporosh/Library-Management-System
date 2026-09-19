using System.Net;
using System.Net.Http.Headers;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.BulkImport;

/// <summary>
/// A downloaded template must import cleanly as-is (its example rows are valid
/// and do not collide with seeded data), and the download itself needs a token.
/// </summary>
public sealed class ImportTemplateRoundTripTests(LibraryApiFactory factory) : IClassFixture<LibraryApiFactory>
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [Fact]
    public async Task Template_download_requires_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/books/import/template");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Untouched_templates_import_successfully_in_dependency_order()
    {
        var client = factory.CreateLibrarianClient();

        foreach (var resource in new[] { "books", "book-copies", "members" })
        {
            var template = await client.GetAsync($"/api/{resource}/import/template");
            Assert.Equal(HttpStatusCode.OK, template.StatusCode);
            Assert.Equal(Xlsx, template.Content.Headers.ContentType?.MediaType);

            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(await template.Content.ReadAsByteArrayAsync());
            file.Headers.ContentType = new MediaTypeHeaderValue(Xlsx);
            form.Add(file, "file", "template.xlsx");

            var import = await client.PostAsync($"/api/{resource}/import", form);

            Assert.True(import.StatusCode == HttpStatusCode.OK,
                $"{resource}: {import.StatusCode} {await import.Content.ReadAsStringAsync()}");
        }
    }
}
