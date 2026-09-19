using Library.Application.Features.Assistant;

namespace Library.UnitTests.Features.Assistant;

public sealed class BookQuestionParserTests
{
    [Theory]
    [InlineData("How many copies of Clean Code are available?", CopyMetric.Available, "Clean Code", null, null, null)]
    [InlineData("how many total copies of \"Clean Code\" do we have", CopyMetric.Total, "Clean Code", null, null, null)]
    [InlineData("How many copies of Clean Code are borrowed?", CopyMetric.Borrowed, "Clean Code", null, null, null)]
    [InlineData("How many borrowed copies of Clean Code have been issued", CopyMetric.Borrowed, "Clean Code", null, null, null)]
    [InlineData("How many copies of books by Robert C. Martin are borrowed?", CopyMetric.Borrowed, null, "Robert C. Martin", null, null)]
    [InlineData("total copies of books written by Eric Evans", CopyMetric.Total, null, "Eric Evans", null, null)]
    [InlineData("How many copies of Refactoring by Martin Fowler are available", CopyMetric.Available, "Refactoring", "Martin Fowler", null, null)]
    [InlineData("how many books are borrowed from publisher Addison-Wesley", CopyMetric.Borrowed, null, null, "Addison-Wesley", null)]
    [InlineData("How many copies of books published by Prentice Hall do we have?", CopyMetric.Total, null, null, "Prentice Hall", null)]
    [InlineData("How many copies of the second edition of Refactoring are borrowed?", CopyMetric.Borrowed, "Refactoring", null, null, "second")]
    [InlineData("how many copies of 20th edition books are available", CopyMetric.Available, null, null, null, "20th")]
    public void Parses_metric_and_criteria(string message, CopyMetric metric, string? title, string? author, string? publisher, string? edition)
    {
        var question = BookQuestionParser.Parse(message);

        Assert.NotNull(question);
        Assert.Equal(metric, question!.Metric);
        Assert.Equal(title, question.Filter.Title);
        Assert.Equal(author, question.Filter.Author);
        Assert.Equal(publisher, question.Filter.Publisher);
        Assert.Equal(edition, question.Filter.Edition);
    }

    [Theory]
    [InlineData("What are the most borrowed books this month?")]
    [InlineData("Who borrowed the most books last month?")]
    [InlineData("What's the weather like?")]
    [InlineData("How many copies?")]
    public void Ignores_questions_that_are_not_copy_counts(string message) =>
        Assert.Null(BookQuestionParser.Parse(message));
}
