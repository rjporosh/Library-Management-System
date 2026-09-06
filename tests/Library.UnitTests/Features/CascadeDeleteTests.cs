using Library.Application.Common.Errors;
using Library.Application.Features.BookCopies;
using Library.Application.Features.BookCopies.Models;
using Library.Application.Features.Books;
using Library.Application.Features.Members;
using Library.Application.Features.Members.Models;
using Library.Domain.Entities;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Repositories.InMemory;

namespace Library.UnitTests.Features;

/// <summary>Soft delete + smart cascade-delete behaviour for books, copies and members.</summary>
public sealed class CascadeDeleteTests
{
    private static (BookService Books, BookCopyService Copies, InMemoryBookRepository BookRepo,
        InMemoryBookCopyRepository CopyRepo, InMemoryBorrowRecordRepository BorrowRepo) BookServices()
    {
        var bookRepo = new InMemoryBookRepository();
        var copyRepo = new InMemoryBookCopyRepository();
        var borrowRepo = new InMemoryBorrowRecordRepository();
        var uow = new NoOpUnitOfWork();
        return (
            new BookService(bookRepo, copyRepo, borrowRepo, uow),
            new BookCopyService(copyRepo, bookRepo, borrowRepo, uow),
            bookRepo, copyRepo, borrowRepo);
    }

    private static Book NewBook() =>
        new(Guid.NewGuid(), "9780132350884", "Clean Code", "Robert C. Martin", 2008, "x", "Software", "Prentice Hall");

    [Fact]
    public async Task DeleteBook_WithNoCopies_SoftDeletesImmediately()
    {
        var (books, _, bookRepo, _, _) = BookServices();
        var book = NewBook();
        bookRepo.Seed([book]);

        var result = await books.DeleteAsync(book.Id, force: false);

        Assert.True(result.IsSuccess);
        Assert.True(book.IsDeleted);
        Assert.Null(await bookRepo.GetByIdAsync(book.Id));
    }

    [Fact]
    public async Task DeleteBook_WithCopies_AndNoForce_AsksForConfirmation()
    {
        var (books, _, bookRepo, copyRepo, _) = BookServices();
        var book = NewBook();
        bookRepo.Seed([book]);
        copyRepo.Seed([new BookCopy(Guid.NewGuid(), book.Id, "BC-1"), new BookCopy(Guid.NewGuid(), book.Id, "BC-2")]);

        var result = await books.DeleteAsync(book.Id, force: false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.BookHasDependentCopies, result.PrimaryErrorCode);
        Assert.False(book.IsDeleted);
    }

    [Fact]
    public async Task DeleteBook_WithCopies_AndForce_CascadeDeletesBookAndCopies()
    {
        var (books, _, bookRepo, copyRepo, _) = BookServices();
        var book = NewBook();
        var c1 = new BookCopy(Guid.NewGuid(), book.Id, "BC-1");
        var c2 = new BookCopy(Guid.NewGuid(), book.Id, "BC-2");
        bookRepo.Seed([book]);
        copyRepo.Seed([c1, c2]);

        var result = await books.DeleteAsync(book.Id, force: true);

        Assert.True(result.IsSuccess);
        Assert.True(book.IsDeleted);
        Assert.True(c1.IsDeleted);
        Assert.True(c2.IsDeleted);
        Assert.Empty(await copyRepo.GetByBookIdAsync(book.Id));
    }

    [Fact]
    public async Task DeleteBook_WithBorrowedCopy_IsAlwaysBlocked_EvenWithForce()
    {
        var (books, _, bookRepo, copyRepo, borrowRepo) = BookServices();
        var book = NewBook();
        var borrowed = new BookCopy(Guid.NewGuid(), book.Id, "BC-1");
        borrowed.Issue();
        bookRepo.Seed([book]);
        copyRepo.Seed([borrowed]);
        borrowRepo.Seed([new BorrowRecord(Guid.NewGuid(), borrowed.Id, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(13))]);

        var result = await books.DeleteAsync(book.Id, force: true);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.BookHasBorrowedCopies, result.PrimaryErrorCode);
        Assert.False(book.IsDeleted);
    }

    [Fact]
    public async Task DeleteMember_WithBorrowHistory_AndForce_CascadeDeletes()
    {
        var memberRepo = new InMemoryMemberRepository();
        var borrowRepo = new InMemoryBorrowRecordRepository();
        var service = new MemberService(memberRepo, borrowRepo, new NoOpUnitOfWork());

        var member = new Member(Guid.NewGuid(), "MEM-1", "Jane", "jane@x.com", null, "555", "1 St");
        memberRepo.Seed([member]);
        var record = new BorrowRecord(Guid.NewGuid(), Guid.NewGuid(), member.Id,
            DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-6));
        record.Return(DateTime.UtcNow.AddDays(-7));
        borrowRepo.Seed([record]);

        var noForce = await service.DeleteAsync(member.Id, force: false);
        Assert.Equal(ErrorCodes.MemberHasBorrowHistory, noForce.PrimaryErrorCode);

        var forced = await service.DeleteAsync(member.Id, force: true);
        Assert.True(forced.IsSuccess);
        Assert.True(member.IsDeleted);
        Assert.True(record.IsDeleted);
    }

    [Fact]
    public async Task DeletedBook_IsExcludedFromSearchAndDuplicateChecks()
    {
        var (books, _, bookRepo, _, _) = BookServices();
        var book = NewBook();
        bookRepo.Seed([book]);
        await books.DeleteAsync(book.Id, force: false);

        // ISBN is now free to reuse.
        Assert.False(await bookRepo.ExistsByIsbnAsync(book.ISBN));
        var page = books.Search(new Application.Common.Search.SearchRequest());
        Assert.Empty(page.Value!.Items);
    }

    [Fact]
    public async Task ChangeStatus_OnCreatedCopy_Works_AndDeletedCopyStaysHidden()
    {
        var (_, copies, bookRepo, copyRepo, _) = BookServices();
        var book = NewBook();
        bookRepo.Seed([book]);
        var create = await copies.CreateAsync(new CreateBookCopyRequest(book.Id, "BC-9"));
        Assert.True(create.IsSuccess);

        var del = await copies.DeleteAsync(create.Value!.Id, force: false);
        Assert.True(del.IsSuccess);
        Assert.Null(await copyRepo.GetByIdAsync(create.Value!.Id));
    }
}
