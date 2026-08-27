using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class BookCopy
{
    public Guid Id { get; init; }

    public Guid BookId { get; init; }

    public string Barcode { get; private set; } = string.Empty;

    public BookCopyStatus Status { get; private set; }

    public BookCopy(
        Guid id,
        Guid bookId,
        string barcode)
    {
        Id = id;
        BookId = bookId;
        Barcode = barcode;
        Status = BookCopyStatus.Available;
    }

    public void Issue()
    {
        if (Status != BookCopyStatus.Available)
            throw new InvalidOperationException("Book copy is not available.");

        Status = BookCopyStatus.Borrowed;
    }

    public void Return()
    {
        if (Status != BookCopyStatus.Borrowed)
            throw new InvalidOperationException("Book copy is not currently borrowed.");

        Status = BookCopyStatus.Available;
    }
}