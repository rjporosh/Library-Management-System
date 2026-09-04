using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Members;
using Library.Application.Features.Members.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.UnitTests.Common;

namespace Library.UnitTests.Features.Members;

public sealed class MemberServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenMemberExists_ShouldReturnMember()
    {
        var member = new Member(
            Guid.NewGuid(),
            "MEM-001",
            "John Doe",
            "john@example.com");

        var repository = new FakeMemberRepository(member);
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var result = await service.GetByIdAsync(member.Id);

        Assert.NotNull(result);
        Assert.Equal(member.Id, result.Id);
        Assert.Equal("MEM-001", result.MembershipNumber);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal(MemberStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMemberDoesNotExist_ShouldReturnNull()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndPersistMember()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var outcome = await service.CreateAsync(
            new CreateMemberRequest(
                "MEM-001",
                "John Doe",
                "john@example.com"));

        Assert.True(outcome.IsSuccess);
        var result = outcome.Value!;

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("MEM-001", result.MembershipNumber);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal(MemberStatus.Active, result.Status);

        Assert.Single(repository.Members);

        var savedMember = repository.Members[0];

        Assert.Equal(result.Id, savedMember.Id);
        Assert.Equal(result.MembershipNumber, savedMember.MembershipNumber);
        Assert.Equal(result.Name, savedMember.Name);
        Assert.Equal(result.Email, savedMember.Email);
        Assert.Equal(MemberStatus.Active, savedMember.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateUniqueMemberId()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var first = await service.CreateAsync(
            new CreateMemberRequest(
                "MEM-001",
                "John Doe",
                "john@example.com"));

        var second = await service.CreateAsync(
            new CreateMemberRequest(
                "MEM-002",
                "Jane Doe",
                "jane@example.com"));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(Guid.Empty, first.Value!.Id);
        Assert.NotEqual(Guid.Empty, second.Value!.Id);
        Assert.NotEqual(first.Value!.Id, second.Value!.Id);

        Assert.Equal(2, repository.Members.Count);
    }

    [Fact]
    public async Task SuspendAsync_WhenMemberExists_ShouldSuspendAndStampTimestamp()
    {
        var member = new Member(
            Guid.NewGuid(),
            "MEM-001",
            "John Doe",
            "john@example.com");

        var repository = new FakeMemberRepository(member);
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var result = await service.SuspendAsync(member.Id);

        Assert.Equal(MemberStatus.Suspended, result.Status);
        Assert.NotNull(result.SuspendedAt);
    }

    [Fact]
    public async Task SuspendAsync_WhenMemberDoesNotExist_ShouldThrow()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.SuspendAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReactivateAsync_WhenMemberIsSuspended_ShouldSetActiveAndClearSuspendedAt()
    {
        var member = new Member(
            Guid.NewGuid(),
            "MEM-001",
            "John Doe",
            "john@example.com");
        member.Suspend();

        var repository = new FakeMemberRepository(member);
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var result = await service.ReactivateAsync(member.Id);

        Assert.Equal(MemberStatus.Active, result.Status);
        Assert.Null(result.SuspendedAt);
    }

    [Fact]
    public async Task RenewAsync_WhenMemberIsSuspended_ShouldSetActiveAndStampRenewal()
    {
        var member = new Member(
            Guid.NewGuid(),
            "MEM-001",
            "John Doe",
            "john@example.com");
        member.Suspend();

        var repository = new FakeMemberRepository(member);
        var service = new MemberService(repository, new StubBorrowRecordRepository());

        var result = await service.RenewAsync(member.Id);

        Assert.Equal(MemberStatus.Active, result.Status);
        Assert.Null(result.SuspendedAt);
        Assert.NotNull(result.LastRenewedAt);
    }

    private sealed class FakeMemberRepository(Member? initialMember = null)
        : IMemberRepository
    {
        public List<Member> Members { get; } =
            initialMember is null
                ? []
                : [initialMember];

        public Task<Member?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Members.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(
            Member member,
            CancellationToken cancellationToken = default)
        {
            Members.Add(member);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Member member,
            CancellationToken cancellationToken = default)
        {
            var index = Members.FindIndex(x => x.Id == member.Id);
            if (index >= 0)
            {
                Members[index] = member;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Member>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Member> members = [.. Members];
            return Task.FromResult(members);
        }

        public IQueryable<Member> Query() => Members.AsQueryable();

        public Task<bool> ExistsByMembershipNumberAsync(string membershipNumber, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Members.Any(x =>
                string.Equals(x.MembershipNumber, membershipNumber, StringComparison.OrdinalIgnoreCase)
                && (excludingId is null || x.Id != excludingId)));

        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Members.Any(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)
                && (excludingId is null || x.Id != excludingId)));

        public Task AddRangeAsync(IEnumerable<Member> members, CancellationToken cancellationToken = default)
        {
            Members.AddRange(members);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Member member, CancellationToken cancellationToken = default)
        {
            Members.RemoveAll(x => x.Id == member.Id);
            return Task.CompletedTask;
        }
    }
}
