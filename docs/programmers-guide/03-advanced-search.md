# Advanced Search

`POST /api/{books|members|book-copies|borrowing}/search`

```json
{
  "filters": [
    { "field": "author", "operator": "contains", "value": "martin" },
    { "field": "publishedYear", "operator": "between", "values": ["2000", "2020"] }
  ],
  "match": "all",                       // "all" = AND, "any" = OR
  "sort": [{ "field": "publishedYear", "direction": "desc" }],
  "page": 1,
  "pageSize": 20,
  "search": "clean"                     // optional free-text over quick-search fields
}
```

**Operators**: `eq, neq, contains, notContains, startsWith, endsWith, gt, gte,
lt, lte, in, notIn, between`. Enum/status fields are matched by **name**
(`"Suspended"`); an unknown field / bad operator / unparseable value returns
`400` with a precise `ApiError` (`SEARCH_FIELD_UNKNOWN` / `_OPERATOR_UNSUPPORTED`
/ `_VALUE_INVALID`) whose `supportedValues` lists the accepted set.

## How it works

- `QueryableSearchBuilder.Apply(IQueryable<T>, SearchRequest, SearchFieldMap<T>)`
  builds `System.Linq.Expressions` from typed accessor lambdas — no
  string-to-code parsing, no dynamic LINQ. A `ParameterRebinder` visitor inlines
  the accessors so the predicate is EF-translatable.
- Each entity has a whitelist: `Features/<F>/<F>SearchMap.cs` —
  `new SearchFieldMap<T>().Field("name", x => x.Name, sortable: true, quickSearch: true)`.
  Anything not in the map cannot be filtered or sorted.
- Filtering is always applied before sorting and paging.

## Adding a searchable field

Add one `.Field(...)` line to the entity's `SearchMap`. Text fields default to
case-insensitive `Contains` via `ToLower()`. Done — the endpoint and the
frontend filter builder pick it up (expose it in the page's `FieldDef[]`).
