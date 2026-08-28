using System.Net;
using System.Net.Http.Json;
using Library.Application.Features.BookCopies.Models;
using Library.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Library.Application.Features.Books.Models;

namespace Library.IntegrationTests.Features.BookCopies;

public sealed class BookCopiesApiTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BookCopiesApiTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetByBookId_ShouldReturnSeededCopies()
    {
        var booksResponse = await _client.GetAsync("/api/Books");

        Assert.Equal(
            HttpStatusCode.OK,
            booksResponse.StatusCode);

    var books =
        await booksResponse.Content.ReadFromJsonAsync<
            PagedBookResponse>();

    Assert.NotNull(books);
    Assert.NotEmpty(books.Items);
    var bookId = books.Items[0].Id;

        var response = await _client.GetAsync(
            $"/api/book-copies/book/{bookId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var copies =
            await response.Content.ReadFromJsonAsync<
                List<BookCopyResponse>>();

        Assert.NotNull(copies);
        Assert.NotEmpty(copies);

        Assert.All(
            copies,
            copy => Assert.Equal(bookId, copy.BookId));
    }

    [Fact]
    public async Task GetById_WhenCopyExists_ShouldReturnCopy()
    {
        var booksResponse = await _client.GetAsync("/api/Books");

        var books =
            await booksResponse.Content.ReadFromJsonAsync<
                PagedBookResponse>();

        Assert.NotNull(books);
        Assert.NotEmpty(books.Items);

        var bookId = books.Items[0].Id;

        var copiesResponse = await _client.GetAsync(
            $"/api/book-copies/book/{bookId}");

        var copies =
            await copiesResponse.Content.ReadFromJsonAsync<
                List<BookCopyResponse>>();

        Assert.NotNull(copies);
        Assert.NotEmpty(copies);

        var expectedCopy = copies[0];

        var response = await _client.GetAsync(
            $"/api/book-copies/{expectedCopy.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var copy =
            await response.Content.ReadFromJsonAsync<
                BookCopyResponse>();

        Assert.NotNull(copy);
        Assert.Equal(expectedCopy.Id, copy.Id);
        Assert.Equal(expectedCopy.BookId, copy.BookId);
        Assert.Equal(expectedCopy.Barcode, copy.Barcode);
        Assert.Equal(
            BookCopyStatus.Available,
            copy.Status);
    }

    [Fact]
    public async Task GetById_WhenCopyDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync(
            $"/api/book-copies/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedCopy()
    {
        var booksResponse = await _client.GetAsync("/api/Books");

        var books =
            await booksResponse.Content.ReadFromJsonAsync<
                PagedBookResponse>();

        Assert.NotNull(books);
        Assert.NotEmpty(books.Items);

        var request = new CreateBookCopyRequest(
            books.Items[0].Id,
            $"BC-TEST-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync(
            "/api/book-copies",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var copy =
            await response.Content.ReadFromJsonAsync<
                BookCopyResponse>();

        Assert.NotNull(copy);
        Assert.NotEqual(Guid.Empty, copy.Id);
        Assert.Equal(request.BookId, copy.BookId);
        Assert.Equal(request.Barcode, copy.Barcode);
        Assert.Equal(
            BookCopyStatus.Available,
            copy.Status);
    }

    [Fact]
    public async Task Create_ShouldMakeCopyRetrievable()
    {
        var booksResponse = await _client.GetAsync("/api/Books");

        var books =
            await booksResponse.Content.ReadFromJsonAsync<
                PagedBookResponse>();

        Assert.NotNull(books);
        Assert.NotEmpty(books.Items);

        var request = new CreateBookCopyRequest(
            books.Items[0].Id,
            $"BC-TEST-{Guid.NewGuid():N}");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/book-copies",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content.ReadFromJsonAsync<
                BookCopyResponse>();

        Assert.NotNull(created);

        var getResponse = await _client.GetAsync(
            $"/api/book-copies/{created.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var retrieved =
            await getResponse.Content.ReadFromJsonAsync<
                BookCopyResponse>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(created.BookId, retrieved.BookId);
        Assert.Equal(created.Barcode, retrieved.Barcode);
        Assert.Equal(created.Status, retrieved.Status);
    }
}
