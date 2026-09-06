using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Seed;

/// <summary>
/// The single canonical demo dataset, shared by the in-memory seeder and the
/// EF Core seeder so both providers present identical data. Covers the QA
/// scenarios the spec asks for: available / borrowed / out-of-service copies,
/// and active / expiring / inactive members, plus one overdue borrow. The
/// exact "Clean Code" / 9780132350884 literals the integration tests assert
/// are preserved.
/// </summary>
public static class SeedData
{
    public sealed record Dataset(
        IReadOnlyList<Book> Books,
        IReadOnlyList<BookCopy> Copies,
        IReadOnlyList<Member> Members,
        IReadOnlyList<BorrowRecord> Borrows);

    public static Dataset Build()
    {
        var now = DateTime.UtcNow;

        var cleanCode = new Book(Guid.NewGuid(), "9780132350884", "Clean Code",
            "Robert C. Martin", 2008, "A handbook of agile software craftsmanship.",
            "Software Engineering", "Prentice Hall");
        var pragmatic = new Book(Guid.NewGuid(), "9780135957059", "The Pragmatic Programmer",
            "David Thomas & Andrew Hunt", 2019, "Your journey to mastery.",
            "Software Engineering", "Addison-Wesley");
        var ddd = new Book(Guid.NewGuid(), "9780321125217", "Domain-Driven Design",
            "Eric Evans", 2003, "Tackling complexity in the heart of software.",
            "Software Architecture", "Addison-Wesley");
        var refactoring = new Book(Guid.NewGuid(), "9780134757599", "Refactoring",
            "Martin Fowler", 2018, "Improving the design of existing code.",
            "Software Engineering", "Addison-Wesley");

        // Every copy of the first-listed book ("Clean Code") stays Available so
        // the copy fixtures are deterministic; borrow from other titles.
        var borrowedCopy = new BookCopy(Guid.NewGuid(), refactoring.Id, "BC-0006");
        var overdueCopy = new BookCopy(Guid.NewGuid(), pragmatic.Id, "BC-0003");
        var lostCopy = new BookCopy(Guid.NewGuid(), ddd.Id, "BC-0005");
        lostCopy.ChangeStatus(BookCopyStatus.Lost);
        borrowedCopy.Issue();
        overdueCopy.Issue();

        var copies = new[]
        {
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0001"),
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0002"),
            overdueCopy,
            new BookCopy(Guid.NewGuid(), pragmatic.Id, "BC-0004"),
            lostCopy,
            borrowedCopy,
            new BookCopy(Guid.NewGuid(), refactoring.Id, "BC-0007"),
        };

        var alice = Member("MEM-100001", "Alice Johnson", "alice@example.com", "+1-202-555-0101", "12 Oak Street, Springfield", now.AddDays(200));
        var bob = Member("MEM-100002", "Bob Smith", "bob@example.com", "+1-202-555-0102", "48 Elm Avenue, Springfield", now.AddDays(15));
        var charlie = Member("MEM-100003", "Charlie Brown", "charlie@example.com", "+1-202-555-0103", "7 Pine Road, Shelbyville", now.AddDays(90));
        var dana = Member("MEM-100004", "Dana Lee", "dana@example.com", "+1-202-555-0104", "90 Maple Lane, Ogdenville", now.AddDays(-10));
        dana.Deactivate();
        var erin = Member("MEM-100005", "Erin Park", "erin@example.com", "+1-202-555-0105", "3 Birch Court, Springfield", now.AddDays(120));

        var borrows = new[]
        {
            new BorrowRecord(Guid.NewGuid(), borrowedCopy.Id, alice.Id, now.AddDays(-3), now.AddDays(11)),
            new BorrowRecord(Guid.NewGuid(), overdueCopy.Id, erin.Id, now.AddDays(-30), now.AddDays(-9)),
        };

        return new Dataset(
            [cleanCode, pragmatic, ddd, refactoring],
            copies,
            [alice, bob, charlie, dana, erin],
            borrows);
    }

    private static Member Member(string number, string name, string email, string phone, string address, DateTime expires) =>
        new(Guid.NewGuid(), number, name, email, expires, phone, address);
}
