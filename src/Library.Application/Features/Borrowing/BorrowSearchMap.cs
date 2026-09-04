using Library.Application.Common.Search;
using Library.Domain.Entities;

namespace Library.Application.Features.Borrowing;

/// <summary>Whitelisted advanced-search fields for borrow records. Status matched by name.</summary>
public static class BorrowSearchMap
{
    public static readonly SearchFieldMap<BorrowRecord> Fields = new SearchFieldMap<BorrowRecord>()
        .Field("memberId", b => b.MemberId)
        .Field("bookCopyId", b => b.BookCopyId)
        .Field("status", b => b.Status)
        .Field("borrowedAt", b => b.BorrowedAt)
        .Field("dueAt", b => b.DueAt)
        .Field("returnedAt", b => b.ReturnedAt);
}
