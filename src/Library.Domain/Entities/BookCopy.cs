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

    /// <summary>Corrects the barcode label. Uniqueness is enforced by the service layer.</summary>
    public void ChangeBarcode(string barcode)
    {
        Barcode = barcode;
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

    /// <summary>
    /// Moves the copy to a non-circulating condition (Lost / Damaged /
    /// Maintenance) or back to Available. A borrowed copy must be
    /// returned before its condition can be changed.
    /// </summary>
    public void ChangeStatus(BookCopyStatus status)
    {
        if (status == BookCopyStatus.Borrowed)
            throw new InvalidOperationException(
                "Use Issue() to lend a copy; 'Borrowed' cannot be set directly.");

        if (Status == BookCopyStatus.Borrowed)
            throw new InvalidOperationException(
                "Return the copy before changing its condition.");

        Status = status;
    }
}