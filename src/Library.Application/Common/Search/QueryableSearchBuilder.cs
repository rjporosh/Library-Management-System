using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Library.Application.Common.Errors;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;

namespace Library.Application.Common.Search;

/// <summary>
/// Turns a <see cref="SearchRequest"/> into a filtered, sorted, paged query
/// over <c>IQueryable&lt;TEntity&gt;</c> using hand-built expression trees.
/// Works identically over an in-memory <c>AsQueryable()</c> and (later) an
/// EF Core query root. Every field and operator is validated against the
/// supplied <see cref="SearchFieldMap{TEntity}"/> - unknown fields, wrong
/// operators and unparseable values fail with a precise
/// <see cref="ApiError"/> rather than throwing. There is no string-to-code
/// parsing, so there is no injection surface.
/// </summary>
public static class QueryableSearchBuilder
{
    private static readonly MethodInfo StringContains =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo StringStartsWith =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    private static readonly MethodInfo StringEndsWith =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    private static readonly MethodInfo StringToLower =
        typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

    public static Result<PagedResult<TEntity>> Apply<TEntity>(
        IQueryable<TEntity> source,
        SearchRequest request,
        SearchFieldMap<TEntity> map)
    {
        var errors = new List<ApiError>();
        var parameter = Expression.Parameter(typeof(TEntity), "e");

        var predicate = BuildFilterPredicate(parameter, request, map, errors);
        ValidateSort(request.Sort, map, errors);

        if (errors.Count > 0)
        {
            return Result.Failure<PagedResult<TEntity>>(errors);
        }

        var query = source;
        if (predicate is not null)
        {
            query = query.Where(Expression.Lambda<Func<TEntity, bool>>(predicate, parameter));
        }

        var totalItems = query.Count();
        query = ApplySort(query, request.Sort, map);

        var pageSize = Math.Clamp(request.PageSize, PaginationDefaults.MinPageSize, PaginationDefaults.MaxPageSize);
        var pageNumber = Math.Max(request.Page, PaginationDefaults.DefaultPageNumber);

        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Result.Success(PagedResult<TEntity>.Create(items, pageNumber, pageSize, totalItems));
    }

    private static Expression? BuildFilterPredicate<TEntity>(
        ParameterExpression parameter,
        SearchRequest request,
        SearchFieldMap<TEntity> map,
        List<ApiError> errors)
    {
        Expression? predicate = null;

        foreach (var filter in request.Filters)
        {
            if (!map.TryGet(filter.Field, out var descriptor))
            {
                errors.Add(UnknownField(filter.Field, map));
                continue;
            }

            var clause = BuildClause(parameter, descriptor, filter, errors);
            if (clause is null)
            {
                continue;
            }

            predicate = Combine(predicate, clause, request.Match);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            Expression? quick = null;
            foreach (var descriptor in map.QuickSearchFields)
            {
                var contains = TextCall(Bind(descriptor.Accessor, parameter), StringContains, term);
                quick = quick is null ? contains : Expression.OrElse(quick, contains);
            }

            if (quick is not null)
            {
                predicate = predicate is null ? quick : Expression.AndAlso(predicate, quick);
            }
        }

        return predicate;
    }

    private static Expression Combine(Expression? left, Expression right, FilterCombinator combinator)
    {
        if (left is null)
        {
            return right;
        }

        return combinator == FilterCombinator.Any
            ? Expression.OrElse(left, right)
            : Expression.AndAlso(left, right);
    }

    private static void ValidateSort<TEntity>(IReadOnlyList<SortSpec> sorts, SearchFieldMap<TEntity> map, List<ApiError> errors)
    {
        foreach (var sort in sorts)
        {
            if (!map.TryGet(sort.Field, out var d))
            {
                errors.Add(UnknownField(sort.Field, map));
            }
            else if (!d.Sortable)
            {
                errors.Add(new ApiError(
                    ErrorCodes.SearchOperatorUnsupported,
                    $"Field '{sort.Field}' cannot be sorted.",
                    sort.Field));
            }
        }
    }

    private static Expression? BuildClause(
        ParameterExpression parameter,
        SearchFieldDescriptor descriptor,
        SearchFilter filter,
        List<ApiError> errors)
    {
        var member = Bind(descriptor.Accessor, parameter);
        var underlying = Nullable.GetUnderlyingType(descriptor.ClrType) ?? descriptor.ClrType;
        var isText = underlying == typeof(string);

        if (filter.Operator is FilterOperator.Contains or FilterOperator.NotContains
                or FilterOperator.StartsWith or FilterOperator.EndsWith
            && !isText)
        {
            errors.Add(new ApiError(
                ErrorCodes.SearchOperatorUnsupported,
                $"Operator '{filter.Operator}' only applies to text fields.",
                filter.Field));
            return null;
        }

        if (filter.Operator is FilterOperator.In or FilterOperator.NotIn)
        {
            return BuildInClause(member, underlying, descriptor, filter, errors);
        }

        if (filter.Operator == FilterOperator.Between)
        {
            return BuildBetweenClause(member, underlying, descriptor, filter, errors);
        }

        var single = filter.Values.Count > 0 ? filter.Values[0] : string.Empty;

        if (isText)
        {
            return BuildTextClause(member, filter, single, errors, descriptor);
        }

        if (!TryParse(single, underlying, out var parsed))
        {
            errors.Add(ValueError(descriptor, filter.Field, single));
            return null;
        }

        var comparable = ToComparable(member, underlying);
        var constant = Expression.Constant(parsed, underlying);

        return filter.Operator switch
        {
            FilterOperator.Eq => Expression.Equal(comparable, constant),
            FilterOperator.Neq => Expression.NotEqual(comparable, constant),
            FilterOperator.Gt => Expression.GreaterThan(comparable, constant),
            FilterOperator.Gte => Expression.GreaterThanOrEqual(comparable, constant),
            FilterOperator.Lt => Expression.LessThan(comparable, constant),
            FilterOperator.Lte => Expression.LessThanOrEqual(comparable, constant),
            _ => Unsupported(errors, filter, descriptor)
        };
    }

    private static Expression? BuildTextClause(
        Expression member, SearchFilter filter, string value, List<ApiError> errors, SearchFieldDescriptor descriptor)
    {
        var lowered = value.ToLowerInvariant();
        return filter.Operator switch
        {
            FilterOperator.Contains => TextCall(member, StringContains, lowered),
            FilterOperator.NotContains => Expression.Not(TextCall(member, StringContains, lowered)),
            FilterOperator.StartsWith => TextCall(member, StringStartsWith, lowered),
            FilterOperator.EndsWith => TextCall(member, StringEndsWith, lowered),
            FilterOperator.Eq => Expression.Equal(LowerOrEmpty(member), Expression.Constant(lowered, typeof(string))),
            FilterOperator.Neq => Expression.NotEqual(LowerOrEmpty(member), Expression.Constant(lowered, typeof(string))),
            _ => Unsupported(errors, filter, descriptor)
        };
    }

    private static Expression? BuildInClause(
        Expression member, Type underlying, SearchFieldDescriptor descriptor, SearchFilter filter, List<ApiError> errors)
    {
        Expression? any = null;
        foreach (var raw in filter.Values)
        {
            if (!TryParse(raw, underlying, out var val))
            {
                errors.Add(ValueError(descriptor, filter.Field, raw));
                return null;
            }

            var eq = Expression.Equal(ToComparable(member, underlying), Expression.Constant(val, underlying));
            any = any is null ? eq : Expression.OrElse(any, eq);
        }

        any ??= Expression.Constant(false);
        return filter.Operator == FilterOperator.NotIn ? Expression.Not(any) : any;
    }

    private static Expression? BuildBetweenClause(
        Expression member, Type underlying, SearchFieldDescriptor descriptor, SearchFilter filter, List<ApiError> errors)
    {
        if (filter.Values.Count != 2 ||
            !TryParse(filter.Values[0], underlying, out var lo) ||
            !TryParse(filter.Values[1], underlying, out var hi))
        {
            errors.Add(new ApiError(
                ErrorCodes.SearchValueInvalid,
                "'between' requires exactly two comparable values.",
                filter.Field, null, null, DescribeType(descriptor)));
            return null;
        }

        var target = ToComparable(member, underlying);
        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(target, Expression.Constant(lo, underlying)),
            Expression.LessThanOrEqual(target, Expression.Constant(hi, underlying)));
    }

    private static Expression TextCall(Expression member, MethodInfo method, string loweredValue)
    {
        var coalesced = Expression.Coalesce(member, Expression.Constant(string.Empty));
        var lowered = Expression.Call(coalesced, StringToLower);
        return Expression.Call(lowered, method, Expression.Constant(loweredValue, typeof(string)));
    }

    private static Expression LowerOrEmpty(Expression member)
    {
        var coalesced = Expression.Coalesce(member, Expression.Constant(string.Empty));
        return Expression.Call(coalesced, StringToLower);
    }

    private static Expression ToComparable(Expression member, Type underlying)
    {
        if (member.Type != underlying && Nullable.GetUnderlyingType(member.Type) == underlying)
        {
            return Expression.Property(member, "Value");
        }

        return member.Type == underlying ? member : Expression.Convert(member, underlying);
    }

    private static Expression Bind(LambdaExpression accessor, ParameterExpression parameter)
    {
        var from = accessor.Parameters[0];
        return new ParameterRebinder(from, parameter).Visit(accessor.Body);
    }

    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }

    private static IQueryable<TEntity> ApplySort<TEntity>(
        IQueryable<TEntity> query,
        IReadOnlyList<SortSpec> sorts,
        SearchFieldMap<TEntity> map)
    {
        IQueryable<TEntity> current = query;
        var first = true;

        foreach (var sort in sorts)
        {
            if (!map.TryGet(sort.Field, out var descriptor))
            {
                continue;
            }

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var body = Bind(descriptor.Accessor, parameter);
            var keySelector = Expression.Lambda(body, parameter);

            var descending = sort.Direction == SortDirection.Desc;
            var method = (first, descending) switch
            {
                (true, false) => "OrderBy",
                (true, true) => "OrderByDescending",
                (false, false) => "ThenBy",
                (false, true) => "ThenByDescending"
            };

            var call = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(TEntity), body.Type],
                current.Expression,
                Expression.Quote(keySelector));

            current = current.Provider.CreateQuery<TEntity>(call);
            first = false;
        }

        return current;
    }

    private static bool TryParse(string? raw, Type target, out object? value)
    {
        raw = raw?.Trim() ?? string.Empty;
        value = null;

        try
        {
            if (target == typeof(string)) { value = raw; return true; }
            if (target.IsEnum) { value = Enum.Parse(target, raw, ignoreCase: true); return true; }
            if (target == typeof(int)) { value = int.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(long)) { value = long.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(decimal)) { value = decimal.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(double)) { value = double.Parse(raw, CultureInfo.InvariantCulture); return true; }
            if (target == typeof(bool)) { value = bool.Parse(raw); return true; }
            if (target == typeof(Guid)) { value = Guid.Parse(raw); return true; }
            if (target == typeof(DateTime))
            {
                value = DateTime.Parse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                return true;
            }

            if (target == typeof(DateOnly)) { value = DateOnly.Parse(raw, CultureInfo.InvariantCulture); return true; }
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            return false;
        }

        return false;
    }

    private static ApiError UnknownField<TEntity>(string field, SearchFieldMap<TEntity> map) =>
        new(ErrorCodes.SearchFieldUnknown,
            $"'{field}' is not a searchable field.",
            field, null, null, string.Join(", ", map.FieldNames));

    private static ApiError ValueError(SearchFieldDescriptor descriptor, string field, string raw) =>
        new(ErrorCodes.SearchValueInvalid,
            $"'{raw}' is not a valid value for '{field}'.",
            field, null, null, DescribeType(descriptor));

    private static string DescribeType(SearchFieldDescriptor descriptor)
    {
        var t = Nullable.GetUnderlyingType(descriptor.ClrType) ?? descriptor.ClrType;
        return t.IsEnum ? string.Join(", ", Enum.GetNames(t)) : t.Name;
    }

    private static Expression? Unsupported(List<ApiError> errors, SearchFilter filter, SearchFieldDescriptor descriptor)
    {
        errors.Add(new ApiError(
            ErrorCodes.SearchOperatorUnsupported,
            $"Operator '{filter.Operator}' is not supported for field '{filter.Field}'.",
            filter.Field, null, null, DescribeType(descriptor)));
        return null;
    }
}
