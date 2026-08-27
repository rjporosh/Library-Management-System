using System.Net;
using System.Net.Http.Json;
using Library.Application.Features.Books.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Library.IntegrationTests.Features.Books;

public sealed class BooksApiTests
{
    [Fact]
    public async Task GetAll_ShouldReturnSeededBooks()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var books = await response.Content
            .ReadFromJsonAsync<List<BookResponse>>();

        Assert.NotNull(books);
        Assert.NotEmpty(books);

        Assert.Contains(
            books,
            book => book.ISBN == "9780132350884"
                && book.Title == "Clean Code"
                && book.Author == "Robert C. Martin");
    }

    [Fact]
    public async Task GetById_WhenBookDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/books/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenBookExists_ShouldReturnBook()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var books = await client.GetFromJsonAsync<List<BookResponse>>(
            "/api/books");

        Assert.NotNull(books);

        var expectedBook = books.First();

        var response = await client.GetAsync(
            $"/api/books/{expectedBook.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var book = await response.Content
            .ReadFromJsonAsync<BookResponse>();

        Assert.NotNull(book);
        Assert.Equal(expectedBook.Id, book.Id);
        Assert.Equal(expectedBook.ISBN, book.ISBN);
        Assert.Equal(expectedBook.Title, book.Title);
        Assert.Equal(expectedBook.Author, book.Author);
        Assert.Equal(expectedBook.Description, book.Description);
        Assert.Equal(expectedBook.PublishedYear, book.PublishedYear);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedBook()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var request = new CreateBookRequest(
            "9780000000001",
            "Integration Testing with ASP.NET Core",
            "Test Author",
            2026,
            "Created by an integration test.");

        var response = await client.PostAsJsonAsync(
            "/api/books",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var book = await response.Content
            .ReadFromJsonAsync<BookResponse>();

        Assert.NotNull(book);
        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal(request.ISBN, book.ISBN);
        Assert.Equal(request.Title, book.Title);
        Assert.Equal(request.Author, book.Author);
        Assert.Equal(request.PublishedYear, book.PublishedYear);
        Assert.Equal(request.Description, book.Description);
    }

    [Fact]
    public async Task Create_ShouldMakeBookRetrievable()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var request = new CreateBookRequest(
            "9780000000002",
            "Retrievable Integration Book",
            "Integration Test Author",
            2026,
            null);

        var createResponse = await client.PostAsJsonAsync(
            "/api/books",
            request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdBook = await createResponse.Content
            .ReadFromJsonAsync<BookResponse>();

        Assert.NotNull(createdBook);

        var getResponse = await client.GetAsync(
            $"/api/books/{createdBook.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrievedBook = await getResponse.Content
            .ReadFromJsonAsync<BookResponse>();

        Assert.NotNull(retrievedBook);
        Assert.Equal(createdBook.Id, retrievedBook.Id);
        Assert.Equal(request.ISBN, retrievedBook.ISBN);
        Assert.Equal(request.Title, retrievedBook.Title);
        Assert.Equal(request.Author, retrievedBook.Author);
        Assert.Equal(request.PublishedYear, retrievedBook.PublishedYear);
        Assert.Null(retrievedBook.Description);
    }
}
