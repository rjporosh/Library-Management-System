using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Common;

/// <summary>Reads identity claims set by <c>JwtTokenService</c> (see Library.Infrastructure.Security).</summary>
public static class CurrentUserExtensions
{
    /// <summary>The signed-in user's id (JWT "sub" claim).</summary>
    public static Guid CurrentUserId(this ControllerBase controller)
    {
        var raw = controller.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? controller.User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    /// <summary>The signed-in Member's id (JWT "memberId" claim). Empty for a Librarian account.</summary>
    public static Guid? CurrentMemberId(this ControllerBase controller)
    {
        var raw = controller.User.FindFirstValue("memberId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
