using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Books;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.UnitTests.Features.Books;

public sealed class BookServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedBooksWithPagination()
    {
        var firstBook = CreateBook(
            title: "Clean Code",
            author: "Robert C. Martin");

        var secondBook = CreateBook(
            title: "Clean Architecture",
            author: "Robert C. Martin");

        var repository = new FakeBookRepository(
            [firstBook, secondBook]);

        var service = new BookService(repository);

var result = await service.GetAllAsync(
    new BookQuery(),
    CancellationToken.None);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(secondBook.Id, result.Items[0].Id);
        Assert.Equal(secondBook.ISBN, result.Items[0].ISBN);
        Assert.Equal(secondBook.Title, result.Items[0].Title);
        Assert.Equal(secondBook.Author, result.Items[0].Author);
        Assert.Equal(secondBook.Description, result.Items[0].Description);
        Assert.Equal(secondBook.PublishedYear, result.Items[0].PublishedYear);

        Assert.Equal(firstBook.Id, result.Items[1].Id);
        Assert.Equal(firstBook.ISBN, result.Items[1].ISBN);
        Assert.Equal(firstBook.Title, result.Items[1].Title);
        Assert.Equal(firstBook.Author, result.Items[1].Author);
        Assert.Equal(firstBook.Description, result.Items[1].Description);
        Assert.Equal(firstBook.PublishedYear, result.Items[1].PublishedYear);           

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryIsEmpty_ShouldReturnEmptyPage()
    {
        var repository = new FakeBookRepository();

        var service = new BookService(repository);

        var result = await service.GetAllAsync(
            new BookQuery(),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_ShouldRespectPagination()
    {
        var books = Enumerable.Range(1, 25)
            .Select(index => CreateBook(
                title: $"Book {index:D2}",
                author: $"Author {index:D2}"))
            .ToList();

        var repository = new FakeBookRepository(books);

        var service = new BookService(repository);

        var result = await service.GetAllAsync(
            new BookQuery(
                PageNumber: 2,
                PageSize: 10));

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSearchTitleByDefault()
    {
        var matchingBook = CreateBook(
            title: "Clean Code",
            author: "Robert C. Martin");

        var nonMatchingBook = CreateBook(
            title: "Design Patterns",
            author: "Clean Author");

        var repository = new FakeBookRepository(
            [matchingBook, nonMatchingBook]);

        var service = new BookService(repository);

        var result = await service.GetAllAsync(
            new BookQuery(
                Search: "  CLEAN  "));

        Assert.Single(result.Items);
        Assert.Equal(matchingBook.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSearchSelectedFieldsUsingOrSemantics()
    {
        var titleMatch = CreateBook(
            title: "Clean Architecture",
            author: "Author One");

        var authorMatch = CreateBook(
            title: "Design Patterns",
            author: "Clean Author");

        var isbnMatch = CreateBook(
            title: "Domain Driven Design",
            author: "Author Three",
            isbn: "978-CLEAN-123");

        var noMatch = CreateBook(
            title: "Refactoring",
            author: "Martin Fowler",
            isbn: "978-1234567890");

        var repository = new FakeBookRepository(
            [titleMatch, authorMatch, isbnMatch, noMatch]);

        var service = new BookService(repository);

        var result = await service.GetAllAsync(
            new BookQuery(
                Search: "clean",
                SearchBy: "title,author,isbn"));

        Assert.Equal(3, result.Items.Count);

        Assert.Contains(
            result.Items,
            book => book.Id == titleMatch.Id);

        Assert.Contains(
            result.Items,
            book => book.Id == authorMatch.Id);

        Assert.Contains(
            result.Items,
            book => book.Id == isbnMatch.Id);

        Assert.DoesNotContain(
            result.Items,
            book => book.Id == noMatch.Id);
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

    private static Book CreateBook(
        string isbn = "978-1234567890",
        string title = "Test Book",
        string author = "Test Author",
        int publishedYear = 2024,
        string? description = "Test description.")
    {
        return new Book(
            Guid.NewGuid(),
            isbn,
            title,
            author,
            publishedYear,
            description);
    }

    private sealed class FakeBookRepository(
        IEnumerable<Book>? books = null) : IBookRepository
    {
        private readonly List<Book> _books =
            books?.ToList() ?? [];

        public IReadOnlyList<Book> Books => _books;

        public Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(
            BookQuery query,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<Book> filteredBooks = _books;

            var search = query.Search?.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchFields = ParseSearchFields(query.SearchBy);

                filteredBooks = filteredBooks.Where(book =>
                    (searchFields.Contains(BookSearchField.Title) &&
                     book.Title.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    (searchFields.Contains(BookSearchField.Author) &&
                     book.Author.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    (searchFields.Contains(BookSearchField.ISBN) &&
                     book.ISBN.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase)));
            }

            var totalItems = filteredBooks.Count();

            var items = filteredBooks
                .OrderBy(book => book.Title)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return Task.FromResult(
                (
                    (IReadOnlyList<Book>)items,
                    totalItems
                ));
        }

        public Task<Book?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                _books.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(
            Book book,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _books.Add(book);

            return Task.CompletedTask;
        }

                public Task UpdateAsync(
            Book book,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
        
            var index = _books.FindIndex(x => x.Id ==       book.Id);
        
            if (index >= 0)
            {
                _books[index] = book;
            }
        
            return Task.CompletedTask;
        }
        
        public Task DeleteAsync(
            Book book,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
        
            _books.RemoveAll(x => x.Id == book.Id);
        
            return Task.CompletedTask;
        }

        private static HashSet<BookSearchField> ParseSearchFields(
            string? searchBy)
        {
            if (string.IsNullOrWhiteSpace(searchBy))
            {
                return [BookSearchField.Title];
            }

            var fields = new HashSet<BookSearchField>();

            foreach (var value in searchBy.Split(
                         ',',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<BookSearchField>(
                        value.Trim(),
                        ignoreCase: true,
                        out var field))
                {
                    fields.Add(field);
                }
            }

            return fields.Count == 0
                ? [BookSearchField.Title]
                : fields;
        }

        private enum BookSearchField
        {
            Title,
            Author,
            ISBN
        }
    }
}