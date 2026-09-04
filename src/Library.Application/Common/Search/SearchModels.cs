namespace Library.Application.Common.Search;

/// <summary>How multiple filters combine.</summary>
public enum FilterCombinator
{
    /// <summary>All filters must match (AND).</summary>
    All,

    /// <summary>Any filter may match (OR).</summary>
    Any
}

/// <summary>Comparison operators the advanced-search endpoints accept.</summary>
public enum FilterOperator
{
    Eq,
    Neq,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    Gt,
    Gte,
    Lt,
    Lte,
    In,
    NotIn,
    Between
}

public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>One filter clause: a whitelisted field, an operator and one or more values.</summary>
public sealed record SearchFilter(
    string Field,
    FilterOperator Operator,
    IReadOnlyList<string> Values);

/// <summary>One sort clause.</summary>
public sealed record SortSpec(string Field, SortDirection Direction);

/// <summary>
/// A GitLab-style advanced query: multiple field/operator/value filters
/// combined with AND or OR, multi-field sort, and pagination. Sorting is
/// always applied after filtering.
/// </summary>
public sealed record SearchRequest
{
    public IReadOnlyList<SearchFilter> Filters { get; init; } = [];

    public FilterCombinator Match { get; init; } = FilterCombinator.All;

    public IReadOnlyList<SortSpec> Sort { get; init; } = [];

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Optional free-text term applied across every text field flagged as quick-searchable.</summary>
    public string? Search { get; init; }
}
