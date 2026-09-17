namespace Library.Domain.Enums;

/// <summary>
/// The two roles supported by the platform. A Librarian manages the catalog,
/// copies, members and borrow/return workflow. A Member can search the
/// catalog and raise borrow/purchase requests for a librarian to action.
/// </summary>
public enum UserRole
{
    Librarian,
    Member
}
