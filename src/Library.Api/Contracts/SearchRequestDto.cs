using Library.Application.Common.Search;

namespace Library.Api.Contracts;

/// <summary>
/// Wire format for the advanced-search endpoints. Operators and directions
/// are strings ("contains", "eq", "desc", ...) so the request reads
/// naturally; <see cref="ToDomain"/> maps it to the internal
/// <see cref="SearchRequest"/>.
/// </summary>
public sealed class SearchRequestDto
{
    public List<FilterDto> Filters { get; set; } = [];

    /// <summary>"all" (AND, default) or "any" (OR).</summary>
    public string Match { get; set; } = "all";

    public List<SortDto> Sort { get; set; } = [];

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }

    public sealed class FilterDto
    {
        public string Field { get; set; } = string.Empty;

        /// <summary>eq, neq, contains, notContains, startsWith, endsWith, gt, gte, lt, lte, in, notIn, between.</summary>
        public string Operator { get; set; } = "eq";

        /// <summary>Single value convenience; folded into <see cref="Values"/>.</summary>
        public string? Value { get; set; }

        public List<string> Values { get; set; } = [];
    }

    public sealed class SortDto
    {
        public string Field { get; set; } = string.Empty;

        /// <summary>"asc" (default) or "desc".</summary>
        public string Direction { get; set; } = "asc";
    }

    public SearchRequest ToDomain()
    {
        var filters = Filters.Select(f =>
        {
            var values = new List<string>(f.Values);
            if (!string.IsNullOrEmpty(f.Value))
            {
                values.Insert(0, f.Value);
            }

            return new SearchFilter(
                f.Field,
                ParseOperator(f.Operator),
                values);
        }).ToList();

        var sort = Sort.Select(s => new SortSpec(
            s.Field,
            s.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Desc
                : SortDirection.Asc)).ToList();

        return new SearchRequest
        {
            Filters = filters,
            Match = Match.Equals("any", StringComparison.OrdinalIgnoreCase)
                ? FilterCombinator.Any
                : FilterCombinator.All,
            Sort = sort,
            Page = Page,
            PageSize = PageSize,
            Search = Search
        };
    }

    private static FilterOperator ParseOperator(string op) => op.Trim().ToLowerInvariant() switch
    {
        "eq" or "=" or "==" => FilterOperator.Eq,
        "neq" or "!=" or "<>" => FilterOperator.Neq,
        "contains" or "like" => FilterOperator.Contains,
        "notcontains" or "notlike" => FilterOperator.NotContains,
        "startswith" => FilterOperator.StartsWith,
        "endswith" => FilterOperator.EndsWith,
        "gt" or ">" => FilterOperator.Gt,
        "gte" or ">=" => FilterOperator.Gte,
        "lt" or "<" => FilterOperator.Lt,
        "lte" or "<=" => FilterOperator.Lte,
        "in" => FilterOperator.In,
        "notin" => FilterOperator.NotIn,
        "between" => FilterOperator.Between,
        _ => FilterOperator.Eq
    };
}
