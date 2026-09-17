using Library.Application.Common.Search;
using Library.Domain.Entities;

namespace Library.Application.Features.BorrowRequests;

/// <summary>Whitelisted advanced-search fields for borrow requests. Status/Type matched by name.</summary>
public static class BorrowRequestSearchMap
{
    public static readonly SearchFieldMap<BorrowRequest> Fields = new SearchFieldMap<BorrowRequest>()
        .Field("memberId", r => r.MemberId)
        .Field("bookId", r => r.BookId)
        .Field("type", r => r.Type)
        .Field("status", r => r.Status)
        .Field("requestedAt", r => r.RequestedAt)
        .Field("decidedAt", r => r.DecidedAt);
}
