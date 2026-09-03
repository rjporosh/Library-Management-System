using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Borrowing;
using Library.Application.Features.Borrowing.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Features.Borrowing;

public sealed class BorrowingServiceTests
{
    [Fact]
    public async Task IssueAsync_ShouldCreateBorrowRecord_AndIssueCopy()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var copy = new BookCopy(
            copyId,
            bookId,
            "BC-001");

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository(copy);
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var dueAt = DateTime.UtcNow.AddDays(14);

        var result = await service.IssueAsync(
            new IssueBookRequest(
                memberId,
                copyId,
                dueAt));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(memberId, result.MemberId);
        Assert.Equal(copyId, result.BookCopyId);
        Assert.Equal(BorrowStatus.Active, result.Status);
        Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
        Assert.Single(borrowRepository.Records);
    }

    [Fact]
    public async Task IssueAsync_WhenMemberDoesNotExist_ShouldThrow()
    {
        var memberRepository = new FakeMemberRepository();
        var copyRepository = new FakeBookCopyRepository();
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddDays(14))));

        Assert.Equal("Member was not found.", exception.Message);
    }

    [Fact]
    public async Task IssueAsync_WhenMemberIsSuspended_ShouldThrow()
    {
        var memberId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        member.Suspend();

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository();
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    memberId,
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddDays(14))));

        Assert.Equal(
            "Member is not allowed to borrow books.",
            exception.Message);
    }

    [Fact]
    public async Task IssueAsync_WhenMemberAlreadyHasActiveBorrow_ShouldThrow()
    {
        var memberId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var existingCopy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-000");
        existingCopy.Issue();

        var existingActiveRecord = new BorrowRecord(
            Guid.NewGuid(),
            existingCopy.Id,
            memberId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(13));

        var newCopy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository(newCopy);
        var borrowRepository = new FakeBorrowRecordRepository(existingActiveRecord);

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    memberId,
                    newCopy.Id,
                    DateTime.UtcNow.AddDays(14))));

        Assert.Contains("one active borrow", exception.Message);
        Assert.Single(borrowRepository.Records);
        Assert.Equal(BookCopyStatus.Available, newCopy.Status);
    }

    [Fact]
    public async Task IssueAsync_WhenCopyDoesNotExist_ShouldThrow()
    {
        var memberId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository();
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    memberId,
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddDays(14))));

        Assert.Equal("Book copy was not found.", exception.Message);
    }

    [Fact]
    public async Task IssueAsync_WhenCopyIsAlreadyBorrowed_ShouldThrow()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var copy = new BookCopy(
            copyId,
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository(copy);
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    memberId,
                    copyId,
                    DateTime.UtcNow.AddDays(14))));
    }

    [Fact]
    public async Task IssueAsync_WhenDueDateIsNotFuture_ShouldThrow()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var copy = new BookCopy(
            copyId,
            Guid.NewGuid(),
            "BC-001");

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository(copy);
        var borrowRepository = new FakeBorrowRecordRepository();

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.IssueAsync(
                new IssueBookRequest(
                    memberId,
                    copyId,
                    DateTime.UtcNow.AddMinutes(-1))));
    }

    [Fact]
    public async Task ReturnAsync_ShouldReturnRecord_AndMakeCopyAvailable()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var member = new Member(
            memberId,
            "MEM-001",
            "John Doe",
            "john@example.com");

        var copy = new BookCopy(
            copyId,
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        var borrowedAt = DateTime.UtcNow.AddDays(-1);
        var dueAt = DateTime.UtcNow.AddDays(13);

        var record = new BorrowRecord(
            Guid.NewGuid(),
            copyId,
            memberId,
            borrowedAt,
            dueAt);

        var memberRepository = new FakeMemberRepository(member);
        var copyRepository = new FakeBookCopyRepository(copy);
        var borrowRepository = new FakeBorrowRecordRepository(record);

        var service = new BorrowingService(
            memberRepository,
            copyRepository,
            borrowRepository);

        var returnedAt = DateTime.UtcNow;

        var result = await service.ReturnAsync(
            record.Id,
            new ReturnBookRequest(returnedAt));

        Assert.Equal(BorrowStatus.Returned, result.Status);
        Assert.Equal(returnedAt, result.ReturnedAt);
        Assert.Equal(BookCopyStatus.Available, copy.Status);
    }

    [Fact]
    public async Task ReturnAsync_WhenRecordDoesNotExist_ShouldThrow()
    {
        var service = new BorrowingService(
            new FakeMemberRepository(),
            new FakeBookCopyRepository(),
            new FakeBorrowRecordRepository());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ReturnAsync(
                Guid.NewGuid(),
                new ReturnBookRequest()));

        Assert.Equal(
            "Borrow record was not found.",
            exception.Message);
    }

    [Fact]
    public async Task ReturnAsync_WhenCopyDoesNotExist_ShouldThrow()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var record = new BorrowRecord(
            Guid.NewGuid(),
            copyId,
            memberId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(13));

        var service = new BorrowingService(
            new FakeMemberRepository(),
            new FakeBookCopyRepository(),
            new FakeBorrowRecordRepository(record));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ReturnAsync(
                record.Id,
                new ReturnBookRequest()));

        Assert.Equal(
            "Book copy was not found.",
            exception.Message);
    }

    [Fact]
    public async Task ReturnAsync_WhenRecordAlreadyReturned_ShouldThrow()
    {
        var memberId = Guid.NewGuid();
        var copyId = Guid.NewGuid();

        var copy = new BookCopy(
            copyId,
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        var record = new BorrowRecord(
            Guid.NewGuid(),
            copyId,
            memberId,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(12));

        record.Return(DateTime.UtcNow.AddDays(-1));

        var service = new BorrowingService(
            new FakeMemberRepository(),
            new FakeBookCopyRepository(copy),
            new FakeBorrowRecordRepository(record));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReturnAsync(
                record.Id,
                new ReturnBookRequest()));
    }

    private sealed class FakeMemberRepository(Member? member = null)
        : IMemberRepository
    {
        public Task<Member?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                member?.Id == id ? member : null);
        }

        public Task AddAsync(
            Member member,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(
            Member member,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Member>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Member> members = member is null ? [] : [member];
            return Task.FromResult(members);
        }
    }

    private sealed class FakeBookCopyRepository(BookCopy? copy = null)
        : IBookCopyRepository
    {
        public Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BookCopy> result = copy is not null &&
                                             copy.BookId == bookId
                ? [copy]
                : [];

            return Task.FromResult(result);
        }

        public Task<BookCopy?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                copy?.Id == id ? copy : null);
        }


        public Task AddAsync(
            BookCopy bookCopy,
            CancellationToken cancellationToken = default)
        {
                cancellationToken.ThrowIfCancellationRequested();
                 return Task.CompletedTask;
        }

        public Task UpdateAsync(
            BookCopy bookCopy,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
    private sealed class FakeBorrowRecordRepository(
        BorrowRecord? initialRecord = null)
        : IBorrowRecordRepository
    {
        public List<BorrowRecord> Records { get; } =
            initialRecord is null
                ? []
                : [initialRecord];

        public Task<BorrowRecord?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Records.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(
            BorrowRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            BorrowRecord record,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> HasActiveBorrowAsync(
            Guid memberId,
            CancellationToken cancellationToken = default)
        {
            var hasActive = Records.Any(x =>
                x.MemberId == memberId &&
                x.Status == BorrowStatus.Active);

            return Task.FromResult(hasActive);
        }

        public Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(
            DateTime asOfUtc,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BorrowRecord> overdue =
                [.. Records.Where(x => x.IsOverdue(asOfUtc))];

            return Task.FromResult(overdue);
        }
    }
}
