using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Members;
using Library.Application.Features.Members.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

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
        var service = new MemberService(repository);

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
        var service = new MemberService(repository);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndPersistMember()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository);

        var result = await service.CreateAsync(
            new CreateMemberRequest(
                "MEM-001",
                "John Doe",
                "john@example.com"));

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
        var service = new MemberService(repository);

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

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);

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
        var service = new MemberService(repository);

        var result = await service.SuspendAsync(member.Id);

        Assert.Equal(MemberStatus.Suspended, result.Status);
        Assert.NotNull(result.SuspendedAt);
    }

    [Fact]
    public async Task SuspendAsync_WhenMemberDoesNotExist_ShouldThrow()
    {
        var repository = new FakeMemberRepository();
        var service = new MemberService(repository);

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
        var service = new MemberService(repository);

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
        var service = new MemberService(repository);

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
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Member>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Member> members = [.. Members];
            return Task.FromResult(members);
        }
    }
}
