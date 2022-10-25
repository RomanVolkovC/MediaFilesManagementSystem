using System.Linq.Expressions;

namespace MediaFilesManagementSystem.Table.Data;

internal class ColumnsFiltersManager<T>
{
    private Expression<Func<T, bool>>? _meetsAllFiltersConditions;

    public IQueryable<T> FilterItems(IQueryable<T> items)
        => _meetsAllFiltersConditions == null ? items : items.Where(_meetsAllFiltersConditions);

    public void ApplyFiltersConditions(IEnumerable<IColumn<T>> columns)
    {
        var columnsWithFilteringCondition = columns.Where(column => column.Filter != null && column.Filter.HasCondition);

        if (columnsWithFilteringCondition.Any())
        {
            var itemParam = Expression.Parameter(typeof(T));
            var invocationExpressions = columnsWithFilteringCondition.Select(column => Expression.Invoke(column.PassesFilter, itemParam));

            BinaryExpression? result = null;
            InvocationExpression firstInvocationExpression = invocationExpressions.First();

            foreach (var invocationExpression in invocationExpressions.Skip(1))
                result = result == null
                    ? Expression.AndAlso(firstInvocationExpression, invocationExpression)
                    : Expression.AndAlso(result, invocationExpression);

            _meetsAllFiltersConditions = Expression.Lambda<Func<T, bool>>(result == null ? firstInvocationExpression : result, itemParam);
        }
        else
        {
            _meetsAllFiltersConditions = null;
        }
    }
}
