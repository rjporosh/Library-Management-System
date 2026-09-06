using Library.Infrastructure.Persistence.Seed;

namespace Library.Infrastructure.Persistence.Repositories.InMemory.Seed;

/// <summary>Loads the canonical <see cref="SeedData"/> dataset into the in-memory repositories.</summary>
public sealed class InMemoryDataSeeder(
    InMemoryBookRepository bookRepository,
    InMemoryBookCopyRepository bookCopyRepository,
    InMemoryMemberRepository memberRepository,
    InMemoryBorrowRecordRepository borrowRecordRepository)
{
    public void Seed()
    {
        var data = SeedData.Build();
        bookRepository.Seed(data.Books);
        bookCopyRepository.Seed(data.Copies);
        memberRepository.Seed(data.Members);
        borrowRecordRepository.Seed(data.Borrows);
    }
}
