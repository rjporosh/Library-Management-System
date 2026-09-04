using Library.Application.Abstractions.Persistence;

namespace Library.Application.Features.Members;

/// <summary>
/// The nightly membership-maintenance pass, shared by the cron job and the
/// manual trigger endpoint so both do exactly the same thing:
///   1. suspend members with an overdue active borrow;
///   2. mark active members whose membership term has expired as Inactive.
/// </summary>
public sealed class MemberMaintenanceService(
    IMemberRepository memberRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<MemberMaintenanceResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var suspended = 0;
        var deactivated = 0;

        var overdue = await borrowRecordRepository.GetOverdueActiveAsync(now, cancellationToken);
        foreach (var memberId in overdue.Select(b => b.MemberId).Distinct())
        {
            var member = await memberRepository.GetByIdAsync(memberId, cancellationToken);
            if (member is null || !member.CanBorrow())
            {
                continue;
            }

            member.Suspend();
            await memberRepository.UpdateAsync(member, cancellationToken);
            suspended++;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var allMembers = await memberRepository.GetAllAsync(cancellationToken);
        var expiredIds = allMembers
            .Where(m => m.Status == Domain.Enums.MemberStatus.Active && m.IsExpired(now))
            .Select(m => m.Id)
            .ToList();

        foreach (var memberId in expiredIds)
        {
            var member = await memberRepository.GetByIdAsync(memberId, cancellationToken);
            if (member is null || member.Status != Domain.Enums.MemberStatus.Active)
            {
                continue;
            }

            member.Deactivate();
            await memberRepository.UpdateAsync(member, cancellationToken);
            deactivated++;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new MemberMaintenanceResult(suspended, deactivated, now);
    }
}

/// <summary>Outcome of one maintenance pass.</summary>
public sealed record MemberMaintenanceResult(int OverdueSuspended, int ExpiredDeactivated, DateTime RanAtUtc);
