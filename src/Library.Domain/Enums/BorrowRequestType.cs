namespace Library.Domain.Enums;

/// <summary>What a member is asking a librarian to do.</summary>
public enum BorrowRequestType
{
    /// <summary>Borrow an existing catalog title.</summary>
    Borrow,

    /// <summary>Suggest the library acquire a title it does not currently have.</summary>
    Purchase
}
