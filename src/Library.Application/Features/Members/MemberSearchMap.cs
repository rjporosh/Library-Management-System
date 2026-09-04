using Library.Application.Common.Search;
using Library.Domain.Entities;

namespace Library.Application.Features.Members;

/// <summary>Whitelisted advanced-search fields for members. Status is matched by name ("Active", "Suspended", "Inactive").</summary>
public static class MemberSearchMap
{
    public static readonly SearchFieldMap<Member> Fields = new SearchFieldMap<Member>()
        .Field("membershipNumber", m => m.MembershipNumber, quickSearch: true)
        .Field("name", m => m.Name, quickSearch: true)
        .Field("email", m => m.Email, quickSearch: true)
        .Field("status", m => m.Status)
        .Field("membershipExpiresAt", m => m.MembershipExpiresAt)
        .Field("suspendedAt", m => m.SuspendedAt)
        .Field("lastRenewedAt", m => m.LastRenewedAt);
}
