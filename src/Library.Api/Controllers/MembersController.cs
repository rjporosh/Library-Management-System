using Library.Application.Features.Members;
using Library.Application.Features.Members.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MembersController(MemberService memberService) : ControllerBase
{
    /// <summary>
    /// Retrieves a library member by their unique identifier.
    /// </summary>
    /// <remarks>
    /// Returns the member's membership number, name, email address,
    /// and current membership status.
    /// </remarks>
    /// <param name="id">The unique identifier of the member.</param>
    /// <response code="200">The member was found successfully.</response>
    /// <response code="404">No member exists with the specified identifier.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(MemberResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var member = await memberService.GetByIdAsync(
            id,
            cancellationToken);

        return member is null
            ? NotFound()
            : Ok(member);
    }

    /// <summary>
    /// Creates a new library member.
    /// </summary>
    /// <remarks>
    /// Creates an active library membership using the supplied
    /// membership number, name, and email address.
    /// </remarks>
    /// <param name="request">The member information to create.</param>
    /// <response code="201">The member was created successfully.</response>
    /// <response code="400">The supplied member information is invalid.</response>
    [HttpPost]
    [ProducesResponseType(
        StatusCodes.Status201Created,
        Type = typeof(MemberResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MemberResponse>> Create(
        CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var member = await memberService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = member.Id },
            member);
    }
}