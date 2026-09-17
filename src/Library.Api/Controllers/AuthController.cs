using Library.Api.Common;
using Library.Application.Features.Auth;
using Library.Application.Features.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>Login and account provisioning. JWT bearer tokens carry the Librarian/Member role.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Logs in with a username/email + password, returning a bearer token.</summary>
    /// <response code="200">The access token and role.</response>
    /// <response code="400">Invalid credentials or inactive account.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        (await authService.LoginAsync(request, cancellationToken)).ToActionResult(this);

    /// <summary>Self-service member sign-up: creates the member profile and an account together.</summary>
    /// <response code="201">Registered and logged in.</response>
    /// <response code="422">Validation failed - every problem is listed.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Register(RegisterMemberRequest request, CancellationToken cancellationToken) =>
        (await authService.RegisterMemberAsync(request, cancellationToken)).ToActionResult(this, StatusCodes.Status201Created);

    /// <summary>Librarian-only: provisions another staff account.</summary>
    /// <response code="201">The new librarian account.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost("librarians")]
    [Authorize(Roles = "Librarian")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> RegisterLibrarian(RegisterLibrarianRequest request, CancellationToken cancellationToken) =>
        (await authService.RegisterLibrarianAsync(request, cancellationToken)).ToActionResult(this, StatusCodes.Status201Created);
}
