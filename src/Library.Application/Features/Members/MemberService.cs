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

    /// <summary>
    /// Suspends a member (e.g. an administrative action, or called by
    /// the member-suspension cron job for overdue borrowers).
    /// </summary>
    public async Task<MemberResponse> SuspendAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Member was not found.");

        member.Suspend();

        await memberRepository.UpdateAsync(member, cancellationToken);

        return Map(member);
    }

    /// <summary>
    /// Sets a suspended (or active) member back to Active without
    /// recording it as a renewal.
    /// </summary>
    public async Task<MemberResponse> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Member was not found.");

        member.Reactivate();

        await memberRepository.UpdateAsync(member, cancellationToken);

        return Map(member);
    }

    /// <summary>
    /// Renews a member's membership: clears suspension and stamps the
    /// renewal date so staff can see when standing was last refreshed.
    /// </summary>
    public async Task<MemberResponse> RenewAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Member was not found.");

        member.Renew();

        await memberRepository.UpdateAsync(member, cancellationToken);

        return Map(member);
    }

    private static MemberResponse Map(Member member)
    {
        return new MemberResponse(
            member.Id,
            member.MembershipNumber,
            member.Name,
            member.Email,
            member.Status,
            member.SuspendedAt,
            member.LastRenewedAt);
    }
}