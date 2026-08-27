using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Books;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.UnitTests.Features.Books;

public sealed class BookServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedBooks()
    {
        var firstBook = CreateBook();
        var secondBook = CreateBook();

        var repository = new FakeBookRepository(
            [firstBook, secondBook]);

        var service = new BookService(repository);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);

        Assert.Equal(firstBook.Id, result[0].Id);
        Assert.Equal(firstBook.ISBN, result[0].ISBN);
        Assert.Equal(firstBook.Title, result[0].Title);
        Assert.Equal(firstBook.Author, result[0].Author);
        Assert.Equal(firstBook.Description, result[0].Description);
        Assert.Equal(firstBook.PublishedYear, result[0].PublishedYear);

        Assert.Equal(secondBook.Id, result[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryIsEmpty_ShouldReturnEmptyList()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository);

        var result = await service.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookExists_ShouldReturnMappedBook()
    {
        var book = CreateBook();
        var repository = new FakeBookRepository([book]);
        var service = new BookService(repository);

        var result = await service.GetByIdAsync(book.Id);

        Assert.NotNull(result);
        Assert.Equal(book.Id, result.Id);
        Assert.Equal(book.ISBN, result.ISBN);
        Assert.Equal(book.Title, result.Title);
        Assert.Equal(book.Author, result.Author);
        Assert.Equal(book.Description, result.Description);
        Assert.Equal(book.PublishedYear, result.PublishedYear);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookDoesNotExist_ShouldReturnNull()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndPersistBook()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository);

        var request = new CreateBookRequest(
            "978-0132350884",
            "Clean Code",
            "Robert C. Martin",
            2008,
            "A handbook of agile software craftsmanship.");

        var result = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.ISBN, result.ISBN);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Author, result.Author);
        Assert.Equal(request.PublishedYear, result.PublishedYear);
        Assert.Equal(request.Description, result.Description);

        Assert.Single(repository.Books);

        var persistedBook = repository.Books[0];

        Assert.Equal(result.Id, persistedBook.Id);
        Assert.Equal(request.ISBN, persistedBook.ISBN);
        Assert.Equal(request.Title, persistedBook.Title);
        Assert.Equal(request.Author, persistedBook.Author);
        Assert.Equal(request.PublishedYear, persistedBook.PublishedYear);
        Assert.Equal(request.Description, persistedBook.Description);
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_ShouldPreserveNullDescription()
    {
        var repository = new FakeBookRepository();
        var service = new BookService(repository);

        var request = new CreateBookRequest(
            "978-1234567890",
            "Test Book",
            "Test Author",
            2024,
            null);

        var result = await service.CreateAsync(request);

        Assert.Null(result.Description);
        Assert.Single(repository.Books);
        Assert.Null(repository.Books[0].Description);
    }

    private static Book CreateBook()
    {
        return new Book(
            Guid.NewGuid(),
            "978-1234567890",
            "Test Book",
            "Test Author",
            2024,
            "Test description.");
    }

    private sealed class FakeBookRepository(
        IEnumerable<Book>? books = null) : IBookRepository
    {
        private readonly List<Book> _books =
            books?.ToList() ?? [];

        public IReadOnlyList<Book> Books => _books;

        public Task<IReadOnlyList<Book>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Book> result = _books.ToList();

            return Task.FromResult(result);
        }

        public Task<Book?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _books.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(
            Book book,
            CancellationToken cancellationToken = default)
        {
            _books.Add(book);

            return Task.CompletedTask;
        }
    }
}
