using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Common.Validation;
using Library.Application.Features.Members.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Members;

public sealed class MemberService(
    IMemberRepository memberRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork)
{
    public Result<PagedResult<MemberResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(memberRepository.Query(), request, MemberSearchMap.Fields);

        if (!result.IsSuccess)
        {
            return Result.Failure<PagedResult<MemberResponse>>(result.Errors);
        }

        var page = result.Value!;
        var memberIds = page.Items.Select(m => m.Id).ToHashSet();
        var activeCounts = borrowRecordRepository.Query()
            .Where(r => memberIds.Contains(r.MemberId) && r.Status == BorrowStatus.Active)
            .GroupBy(r => r.MemberId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Result.Success(page.Map(m => Map(m, activeCounts.GetValueOrDefault(m.Id))));
    }

    public async Task<MemberResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        return member is null ? null : Map(member);
    }

    public async Task<MemberDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var records = await borrowRecordRepository.GetByMemberIdAsync(id, cancellationToken);
        var now = DateTime.UtcNow;

        var history = records
            .Select(r => new MemberBorrowSummary(
                r.Id, r.BookCopyId, r.BorrowedAt, r.DueAt, r.ReturnedAt, r.Status, r.IsOverdue(now)))
            .ToList();

        return new MemberDetailResponse(
            Map(member),
            history.Count,
            history.Count(h => h.Status == BorrowStatus.Active),
            history.Count(h => h.IsOverdue),
            history.Count > 0 ? history.Max(h => h.BorrowedAt) : null,
            history);
    }

    public async Task<Result<MemberResponse>> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>(MemberValidator.Validate(
            new MemberCandidate(request.MembershipNumber, request.Name, request.Email, request.Phone, request.Address)));

        await AddDuplicateErrorsAsync(errors, request.MembershipNumber, request.Email, null, cancellationToken);

        if (errors.Count > 0)
        {
            return Result.Failure<MemberResponse>(errors);
        }

        var member = new Member(
            Guid.NewGuid(), request.MembershipNumber.Trim(), request.Name.Trim(), request.Email.Trim(),
            membershipExpiresAt: null, phone: request.Phone.Trim(), address: request.Address.Trim());

        await memberRepository.AddAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(member));
    }

    public async Task<Result<MemberResponse>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return Result.Failure<MemberResponse>(new ApiError(ErrorCodes.MemberNotFound, "Member was not found.", "id"));
        }

        var errors = new List<ApiError>(MemberValidator.Validate(
            new MemberCandidate(request.MembershipNumber, request.Name, request.Email, request.Phone, request.Address)));

        await AddDuplicateErrorsAsync(errors, request.MembershipNumber, request.Email, id, cancellationToken);

        if (errors.Count > 0)
        {
            return Result.Failure<MemberResponse>(errors);
        }

        member.UpdateProfile(
            request.MembershipNumber.Trim(), request.Name.Trim(), request.Email.Trim(),
            request.Phone.Trim(), request.Address.Trim());

        await memberRepository.UpdateAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(member));
    }

    /// <summary>
    /// Deletes a member (soft delete). Smart cascade:
    ///  - an active borrow blocks the delete, always;
    ///  - borrow history exists and <paramref name="force"/> is false -> asks the
    ///    caller to confirm (returns MEMBER_HAS_BORROW_HISTORY);
    ///  - else soft-deletes the member and their borrow records in one transaction.
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, bool force = false, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new ApiError(ErrorCodes.MemberNotFound, "Member was not found.", "id"));
        }

        if (await borrowRecordRepository.HasActiveBorrowAsync(id, cancellationToken))
        {
            return Result.Failure(new ApiError(ErrorCodes.MemberHasActiveBorrow,
                $"{member.Name} has an active borrowed book and cannot be deleted. Process the return first.", "id"));
        }

        var history = await borrowRecordRepository.GetByMemberIdAsync(id, cancellationToken);
        if (history.Count > 0 && !force)
        {
            return Result.Failure(new ApiError(
                ErrorCodes.MemberHasBorrowHistory,
                $"{member.Name} has {history.Count} borrow record(s). Deleting the member will also remove " +
                "that history. Confirm to proceed.",
                "id"));
        }

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        foreach (var record in history)
        {
            await borrowRecordRepository.DeleteAsync(record, cancellationToken);
        }

        await memberRepository.DeleteAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Result.Success();
    }

    public Task<MemberResponse> SuspendAsync(Guid id, CancellationToken ct = default) =>
        TransitionAsync(id, m => m.Suspend(), ct);

    public Task<MemberResponse> ReactivateAsync(Guid id, CancellationToken ct = default) =>
        TransitionAsync(id, m => m.Reactivate(), ct);

    public Task<MemberResponse> RenewAsync(Guid id, CancellationToken ct = default) =>
        TransitionAsync(id, m => m.Renew(), ct);

    public Task<MemberResponse> DeactivateAsync(Guid id, CancellationToken ct = default) =>
        TransitionAsync(id, m => m.Deactivate(), ct);

    private async Task<MemberResponse> TransitionAsync(Guid id, Action<Member> transition, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Member was not found.");

        transition(member);
        await memberRepository.UpdateAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(member);
    }

    private async Task AddDuplicateErrorsAsync(
        List<ApiError> errors, string membershipNumber, string email, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (errors.Count > 0)
        {
            return;
        }

        if (await memberRepository.ExistsByMembershipNumberAsync(membershipNumber, excludingId, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.MemberNumberDuplicate,
                $"Membership number '{membershipNumber}' is already in use.", "membershipNumber"));
        }

        if (await memberRepository.ExistsByEmailAsync(email, excludingId, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.MemberEmailDuplicate,
                $"Email '{email}' is already in use.", "email"));
        }
    }

    private static MemberResponse Map(Member member, int currentlyBorrowed = 0) =>
        new(member.Id, member.MembershipNumber, member.Name, member.Email, member.Phone, member.Address,
            member.Status, member.MembershipExpiresAt, member.SuspendedAt, member.LastRenewedAt, currentlyBorrowed);
}
