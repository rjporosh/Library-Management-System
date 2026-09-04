using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Exceptions;
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
    IBorrowRecordRepository borrowRecordRepository)
{
    public Result<PagedResult<MemberResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(memberRepository.Query(), request, MemberSearchMap.Fields);

        return result.IsSuccess
            ? Result.Success(result.Value!.Map(Map))
            : Result.Failure<PagedResult<MemberResponse>>(result.Errors);
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
        var errors = new List<ApiError>(
            MemberValidator.Validate(new MemberCandidate(request.MembershipNumber, request.Name, request.Email)));

        if (errors.Count == 0)
        {
            if (await memberRepository.ExistsByMembershipNumberAsync(request.MembershipNumber, null, cancellationToken))
            {
                errors.Add(new ApiError(ErrorCodes.MemberNumberDuplicate,
                    $"Membership number '{request.MembershipNumber}' is already in use.", "membershipNumber"));
            }

            if (await memberRepository.ExistsByEmailAsync(request.Email, null, cancellationToken))
            {
                errors.Add(new ApiError(ErrorCodes.MemberEmailDuplicate,
                    $"Email '{request.Email}' is already in use.", "email"));
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<MemberResponse>(errors);
        }

        var member = new Member(Guid.NewGuid(), request.MembershipNumber.Trim(), request.Name.Trim(), request.Email.Trim());
        await memberRepository.AddAsync(member, cancellationToken);
        return Result.Success(Map(member));
    }

    public async Task<Result<MemberResponse>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return Result.Failure<MemberResponse>(new ApiError(ErrorCodes.MemberNotFound, "Member was not found.", "id"));
        }

        var errors = new List<ApiError>(
            MemberValidator.Validate(new MemberCandidate(request.MembershipNumber, request.Name, request.Email)));

        if (errors.Count == 0)
        {
            if (await memberRepository.ExistsByMembershipNumberAsync(request.MembershipNumber, id, cancellationToken))
            {
                errors.Add(new ApiError(ErrorCodes.MemberNumberDuplicate,
                    $"Membership number '{request.MembershipNumber}' is already in use.", "membershipNumber"));
            }

            if (await memberRepository.ExistsByEmailAsync(request.Email, id, cancellationToken))
            {
                errors.Add(new ApiError(ErrorCodes.MemberEmailDuplicate,
                    $"Email '{request.Email}' is already in use.", "email"));
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<MemberResponse>(errors);
        }

        member.UpdateProfile(request.MembershipNumber.Trim(), request.Name.Trim(), request.Email.Trim());
        await memberRepository.UpdateAsync(member, cancellationToken);
        return Result.Success(Map(member));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return Result.Failure(new ApiError(ErrorCodes.MemberNotFound, "Member was not found.", "id"));
        }

        if (await borrowRecordRepository.HasActiveBorrowAsync(id, cancellationToken))
        {
            return Result.Failure(new ApiError(ErrorCodes.MemberHasActiveBorrow,
                "This member has an active borrowed book and cannot be deleted.", "id"));
        }

        await memberRepository.DeleteAsync(member, cancellationToken);
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
        return Map(member);
    }

    private static MemberResponse Map(Member member) =>
        new(member.Id, member.MembershipNumber, member.Name, member.Email,
            member.Status, member.MembershipExpiresAt, member.SuspendedAt, member.LastRenewedAt);
}
