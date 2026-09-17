using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Application.Features.Books.Models;
using Library.IntegrationTests.Common;

namespace Library.IntegrationTests.Features.Books;

public sealed class BookDetailApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task GetDetail_ForBookWithNoAvailableCopy_SuggestsExternalBuyLink()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var books = await client.GetFromJsonAsync<PagedBookResponse>("/api/books?pageSize=50");
        var ddd = books!.Items.Single(b => b.Title == "Domain-Driven Design");

        var response = await client.GetAsync($"/api/books/{ddd.Id}/detail");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(BookAvailabilityStatus.Unavailable, detail!.Availability.Status);
        Assert.Equal(0, detail.Availability.AvailableCopies);
        Assert.False(string.IsNullOrWhiteSpace(detail.Availability.ExternalBuyUrl));
    }

    [Fact]
    public async Task GetDetail_ForBookWithAvailableCopy_ReportsPhysicalAvailable()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var books = await client.GetFromJsonAsync<PagedBookResponse>("/api/books?pageSize=50");
        var cleanCode = books!.Items.Single(b => b.Title == "Clean Code");

        var response = await client.GetAsync($"/api/books/{cleanCode.Id}/detail");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(BookAvailabilityStatus.PhysicalAvailable, detail!.Availability.Status);
        Assert.True(detail.Availability.AvailableCopies > 0);
    }

    [Fact]
    public async Task Create_WithTotalCopies_GeneratesSequentialBarcodes()
    {
        await using var factory = new LibraryApiFactory();
        using var client = factory.CreateLibrarianClient();

        var request = new CreateBookRequest(
            "9780000000099", "Auto-Copy Book", "Test Author", 2026,
            "Testing", "CI Press", TotalCopies: 3);

        var createResponse = await client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var book = await createResponse.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(book);

        var detailResponse = await client.GetAsync($"/api/books/{book!.Id}/detail");
        var detail = await detailResponse.Content.ReadFromJsonAsync<BookDetailResponse>(JsonOptions);

        Assert.NotNull(detail);
        Assert.Equal(3, detail!.Availability.TotalCopies);
        Assert.Equal(3, detail.Availability.AvailableCopies);

        var copies = await client.GetFromJsonAsync<JsonElement>($"/api/book-copies?bookId={book.Id}&pageSize=10");
        var barcodes = copies.GetProperty("items").EnumerateArray()
            .Select(c => c.GetProperty("barcode").GetString())
            .OrderBy(b => b)
            .ToList();

        Assert.Equal(3, barcodes.Count);
        Assert.All(barcodes, b => Assert.StartsWith("BC-", b));
        Assert.Equal(barcodes.Distinct().Count(), barcodes.Count);
    }
}
