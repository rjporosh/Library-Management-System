using Library.Application.Abstractions.Persistence;
using Library.Application.Features.BookCopies;
using Library.Application.Features.BookCopies.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Features.BookCopies;

public sealed class BookCopyServiceTests
{
    [Fact]
    public async Task GetByBookIdAsync_ShouldReturnCopiesForBook()
    {
        var bookId = Guid.NewGuid();

        var copies =
            new[]
            {
                new BookCopy(Guid.NewGuid(), bookId, "BC-001"),
                new BookCopy(Guid.NewGuid(), bookId, "BC-002"),
                new BookCopy(Guid.NewGuid(), Guid.NewGuid(), "BC-003")
            };

        var repository = new FakeBookCopyRepository(copies);
        var service = new BookCopyService(repository);

        var result = await service.GetByBookIdAsync(bookId);

        Assert.Equal(2, result.Count);
        Assert.All(result, copy => Assert.Equal(bookId, copy.BookId));
        Assert.Equal("BC-001", result[0].Barcode);
        Assert.Equal("BC-002", result[1].Barcode);
    }

    [Fact]
    public async Task GetByBookIdAsync_WhenNoCopiesExist_ShouldReturnEmptyList()
    {
        var repository = new FakeBookCopyRepository();
        var service = new BookCopyService(repository);

        var result = await service.GetByBookIdAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCopyExists_ShouldReturnCopy()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        var repository = new FakeBookCopyRepository([copy]);
        var service = new BookCopyService(repository);

        var result = await service.GetByIdAsync(copy.Id);

        Assert.NotNull(result);
        Assert.Equal(copy.Id, result.Id);
        Assert.Equal(copy.BookId, result.BookId);
        Assert.Equal(copy.Barcode, result.Barcode);
        Assert.Equal(BookCopyStatus.Available, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCopyDoesNotExist_ShouldReturnNull()
    {
        var repository = new FakeBookCopyRepository();
        var service = new BookCopyService(repository);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndPersistCopy()
    {
        var bookId = Guid.NewGuid();
        var repository = new FakeBookCopyRepository();
        var service = new BookCopyService(repository);

        var result = await service.CreateAsync(
            new CreateBookCopyRequest(
                bookId,
                "BC-001"));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(bookId, result.BookId);
        Assert.Equal("BC-001", result.Barcode);
        Assert.Equal(BookCopyStatus.Available, result.Status);

        Assert.Single(repository.Copies);

        var savedCopy = repository.Copies[0];
        Assert.Equal(result.Id, savedCopy.Id);
        Assert.Equal(bookId, savedCopy.BookId);
        Assert.Equal("BC-001", savedCopy.Barcode);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMapBorrowedStatus()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        var repository = new FakeBookCopyRepository([copy]);
        var service = new BookCopyService(repository);

        var result = await service.GetByIdAsync(copy.Id);

        Assert.NotNull(result);
        Assert.Equal(BookCopyStatus.Borrowed, result.Status);
    }

    private sealed class FakeBookCopyRepository(
        IEnumerable<BookCopy>? initialCopies = null)
        : IBookCopyRepository
    {
        public List<BookCopy> Copies { get; } =
            initialCopies?.ToList() ?? [];

        public Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BookCopy> result = Copies
                .Where(x => x.BookId == bookId)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<BookCopy?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Copies.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(
            BookCopy bookCopy,
            CancellationToken cancellationToken = default)
        {
            Copies.Add(bookCopy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            BookCopy bookCopy,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
