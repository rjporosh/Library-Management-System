using System.Linq.Expressions;

namespace Library.Application.Common.Search;

/// <summary>A single whitelisted, typed, filterable/sortable field on an entity.</summary>
public sealed class SearchFieldDescriptor
{
    public required string Name { get; init; }

    public required Type ClrType { get; init; }

    /// <summary>Boxed <c>Expression&lt;Func&lt;TEntity, object&gt;&gt;</c>-style accessor to the property.</summary>
    public required LambdaExpression Accessor { get; init; }

    public bool Sortable { get; init; } = true;

    /// <summary>Included in the free-text quick search (text fields only).</summary>
    public bool QuickSearch { get; init; }
}

/// <summary>
/// The whitelist of fields the advanced-search builder is allowed to touch
/// for a given entity. Nothing outside this map can be filtered or sorted -
/// there is no string-to-expression parsing and therefore no injection
/// surface (spec §5, §7).
/// </summary>
public sealed class SearchFieldMap<TEntity>
{
    private readonly Dictionary<string, SearchFieldDescriptor> _fields =
        new(StringComparer.OrdinalIgnoreCase);

    public SearchFieldMap<TEntity> Field<TProp>(
        string name,
        Expression<Func<TEntity, TProp>> accessor,
        bool sortable = true,
        bool quickSearch = false)
    {
        _fields[name] = new SearchFieldDescriptor
        {
            Name = name,
            ClrType = typeof(TProp),
            Accessor = accessor,
            Sortable = sortable,
            QuickSearch = quickSearch
        };

        return this;
    }

    public bool TryGet(string name, out SearchFieldDescriptor descriptor) =>
        _fields.TryGetValue(name, out descriptor!);

    public IReadOnlyCollection<string> FieldNames => _fields.Keys;

    public IEnumerable<SearchFieldDescriptor> QuickSearchFields =>
        _fields.Values.Where(f => f.QuickSearch);
}
