using Library.Application.Features.Books;

namespace Library.Api.Endpoints;

public static class BooksEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/books")
            .WithTags("Books");

        group.MapGet(
                "/",
                async (
                    BookService bookService,
                    CancellationToken cancellationToken) =>
                {
                    var books = await bookService.GetAllAsync(cancellationToken);

                    return Results.Ok(books);
                })
            .WithName("GetBooks")
            .WithSummary("Get all books")
            .WithDescription(
                "Returns all books currently available in the library catalog.")
            .Produces(200);

        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    BookService bookService,
                    CancellationToken cancellationToken) =>
                {
                    var book = await bookService.GetByIdAsync(
                        id,
                        cancellationToken);

                    return book is null
                        ? Results.NotFound()
                        : Results.Ok(book);
                })
            .WithName("GetBookById")
            .WithSummary("Get a book by ID")
            .WithDescription(
                "Returns a single book from the library catalog using its unique identifier.")
            .Produces(200)
            .Produces(404);

        return endpoints;
    }
}