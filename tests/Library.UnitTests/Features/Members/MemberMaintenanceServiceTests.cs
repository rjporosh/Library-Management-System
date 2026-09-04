using Library.Application.Features.Members;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Persistence.Repositories.InMemory;

using Library.Infrastructure.Persistence;

namespace Library.UnitTests.Features.Members;

public sealed class MemberMaintenanceServiceTests
{
    [Fact]
    public async Task RunAsync_SuspendsOverdueBorrowersAndDeactivatesExpiredMembers()
    {
        var members = new InMemoryMemberRepository();
        var borrows = new InMemoryBorrowRecordRepository();

        var overdueBorrower = new Member(Guid.NewGuid(), "M1", "Overdue", "o@x.com", DateTime.UtcNow.AddDays(30));
        var expired = new Member(Guid.NewGuid(), "M2", "Expired", "e@x.com", DateTime.UtcNow.AddDays(-1));
        var healthy = new Member(Guid.NewGuid(), "M3", "Healthy", "h@x.com", DateTime.UtcNow.AddDays(30));
        members.Seed([overdueBorrower, expired, healthy]);

        borrows.Seed([new BorrowRecord(Guid.NewGuid(), Guid.NewGuid(), overdueBorrower.Id,
            DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-5))]);

        var result = await new MemberMaintenanceService(members, borrows, new NoOpUnitOfWork()).RunAsync();

        Assert.Equal(1, result.OverdueSuspended);
        Assert.Equal(1, result.ExpiredDeactivated);
        Assert.Equal(MemberStatus.Suspended, (await members.GetByIdAsync(overdueBorrower.Id))!.Status);
        Assert.Equal(MemberStatus.Inactive, (await members.GetByIdAsync(expired.Id))!.Status);
        Assert.Equal(MemberStatus.Active, (await members.GetByIdAsync(healthy.Id))!.Status);
    }

    [Fact]
    public async Task RunAsync_LeavesAlreadySuspendedMembersUntouched()
    {
        var members = new InMemoryMemberRepository();
        var borrows = new InMemoryBorrowRecordRepository();

        var suspended = new Member(Guid.NewGuid(), "M1", "S", "s@x.com", DateTime.UtcNow.AddDays(-1));
        suspended.Suspend();
        members.Seed([suspended]);

        var result = await new MemberMaintenanceService(members, borrows, new NoOpUnitOfWork()).RunAsync();

        Assert.Equal(0, result.ExpiredDeactivated);
        Assert.Equal(MemberStatus.Suspended, (await members.GetByIdAsync(suspended.Id))!.Status);
    }
}
