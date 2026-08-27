using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Domain;

public sealed class BookCopyTests
{
    [Fact]
    public void NewCopy_ShouldBeAvailable()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        Assert.Equal(BookCopyStatus.Available, copy.Status);
    }

    [Fact]
    public void Issue_ShouldMarkCopyAsBorrowed()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        Assert.Equal(BookCopyStatus.Borrowed, copy.Status);
    }

    [Fact]
    public void Return_ShouldMarkCopyAsAvailable()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();
        copy.Return();

        Assert.Equal(BookCopyStatus.Available, copy.Status);
    }

    [Fact]
    public void Issue_WhenAlreadyBorrowed_ShouldThrow()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        copy.Issue();

        Assert.Throws<InvalidOperationException>(
            () => copy.Issue());
    }

    [Fact]
    public void Return_WhenAvailable_ShouldThrow()
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BC-001");

        Assert.Throws<InvalidOperationException>(
            () => copy.Return());
    }
}
