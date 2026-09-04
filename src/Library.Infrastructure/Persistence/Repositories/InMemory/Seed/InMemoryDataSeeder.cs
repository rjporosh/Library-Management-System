using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Repositories.InMemory.Seed;

/// <summary>
/// Deterministic demo data for the in-memory provider. Covers the QA
/// scenarios the spec calls for: available / borrowed / out-of-service
/// copies, and active / suspended / inactive / expiring members, plus one
/// overdue borrow.
/// </summary>
public sealed class InMemoryDataSeeder(
    InMemoryBookRepository bookRepository,
    InMemoryBookCopyRepository bookCopyRepository,
    InMemoryMemberRepository memberRepository,
    InMemoryBorrowRecordRepository borrowRecordRepository)
{
    public void Seed()
    {
        var now = DateTime.UtcNow;

        var cleanCode = new Book(Guid.NewGuid(), "9780132350884", "Clean Code",
            "Robert C. Martin", 2008, "A handbook of agile software craftsmanship.");
        var pragmatic = new Book(Guid.NewGuid(), "9780135957059", "The Pragmatic Programmer",
            "David Thomas & Andrew Hunt", 2019, "Your journey to mastery.");
        var ddd = new Book(Guid.NewGuid(), "9780321125217", "Domain-Driven Design",
            "Eric Evans", 2003, "Tackling complexity in the heart of software.");
        var refactoring = new Book(Guid.NewGuid(), "9780134757599", "Refactoring",
            "Martin Fowler", 2018, "Improving the design of existing code.");

        bookRepository.Seed([cleanCode, pragmatic, ddd, refactoring]);

        // Keep every copy of the first-listed book ("Clean Code") Available so
        // the copy fixtures stay deterministic; borrow from other titles.
        var borrowedCopy = new BookCopy(Guid.NewGuid(), refactoring.Id, "BC-0006");
        var overdueCopy = new BookCopy(Guid.NewGuid(), pragmatic.Id, "BC-0003");
        var lostCopy = new BookCopy(Guid.NewGuid(), ddd.Id, "BC-0005");
        lostCopy.ChangeStatus(BookCopyStatus.Lost);

        var copies = new[]
        {
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0001"),
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0002"),
            overdueCopy,
            new BookCopy(Guid.NewGuid(), pragmatic.Id, "BC-0004"),
            lostCopy,
            borrowedCopy,
            new BookCopy(Guid.NewGuid(), refactoring.Id, "BC-0007")
        };

        var alice = new Member(Guid.NewGuid(), "MEM-100001", "Alice Johnson", "alice@example.com", now.AddDays(200));
        var bob = new Member(Guid.NewGuid(), "MEM-100002", "Bob Smith", "bob@example.com", now.AddDays(15));   // expiring soon
        var charlie = new Member(Guid.NewGuid(), "MEM-100003", "Charlie Brown", "charlie@example.com", now.AddDays(90));
        var dana = new Member(Guid.NewGuid(), "MEM-100004", "Dana Lee", "dana@example.com", now.AddDays(-10)); // expired
        dana.Deactivate();
        var erin = new Member(Guid.NewGuid(), "MEM-100005", "Erin Park", "erin@example.com", now.AddDays(120));

        memberRepository.Seed([alice, bob, charlie, dana, erin]);

        // Alice has an active borrow; Erin has an overdue one -> the nightly job would suspend Erin.
        borrowedCopy.Issue();
        overdueCopy.Issue();
        bookCopyRepository.Seed(copies);

        borrowRecordRepository.Seed(
        [
            new BorrowRecord(Guid.NewGuid(), borrowedCopy.Id, alice.Id, now.AddDays(-3), now.AddDays(11)),
            new BorrowRecord(Guid.NewGuid(), overdueCopy.Id, erin.Id, now.AddDays(-30), now.AddDays(-9))
        ]);
    }
}
