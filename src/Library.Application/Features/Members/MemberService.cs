using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Members.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Members;

public sealed class MemberService(IMemberRepository memberRepository)
{
    public async Task<MemberResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(
            id,
            cancellationToken);

        return member is null ? null : Map(member);
    }

    public async Task<MemberResponse> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var member = new Member(
            Guid.NewGuid(),
            request.MembershipNumber,
            request.Name,
            request.Email);

        await memberRepository.AddAsync(
            member,
            cancellationToken);

        return Map(member);
    }

    private static MemberResponse Map(Member member)
    {
        return new MemberResponse(
            member.Id,
            member.MembershipNumber,
            member.Name,
            member.Email,
            member.Status);
    }
}