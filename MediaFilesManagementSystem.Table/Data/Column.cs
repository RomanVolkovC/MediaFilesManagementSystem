using MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

using System.Linq.Expressions;

namespace MediaFilesManagementSystem.Table.Data;

public class Column<TItem, TItemValue> : IColumn<TItem>
    where TItemValue : IComparable<TItemValue>, IComparable
{
    private bool _isHidden;

    public Column(string headerName,
        Action<TItem>? onClick,
        Dictionary<string, object>? cellAttributes,
        Expression<Func<TItem, TItemValue>> getItemValue,
        Func<TItemValue, string>? getDisplayedItemValue,
        Filter<TItemValue>? filter)
    {
        HeaderName = headerName;
        OnClick = onClick;
        CellAttributes = cellAttributes;
        GetItemValue = getItemValue;
        var getItemValueFunc = getItemValue.Compile();
        GetDisplayedItemValue = getDisplayedItemValue == null
            ? item => getItemValueFunc(item).ToString() ?? string.Empty
            : item => getDisplayedItemValue(getItemValueFunc(item));
        Filter = filter;
    }

    public Column(string headerName, Expression<Func<TItem, TItemValue>> getItemValue, Func<TItemValue, string>? getDisplayedItemValue, Filter<TItemValue>? filter)
        : this(headerName, null, null, getItemValue, getDisplayedItemValue, filter) { }

    public Column(string headerName, Expression<Func<TItem, TItemValue>> getItemValue, Filter<TItemValue>? filter)
        : this(headerName, null, null, getItemValue, null, filter) { }

    public event EventHandler? VisibilityChanged;

    public Expression<Func<TItem, bool>> PassesFilter
    {
        get
        {
            if (Filter == null)
            {
                return tItem => true;
            }
            else
            {
                var tItemParam = Expression.Parameter(typeof(TItem));
                var tItemValue = Expression.Invoke(GetItemValue, tItemParam);
                var passesFilter = Expression.Invoke(Filter.MeetsCondition, tItemValue);

                return Expression.Lambda<Func<TItem, bool>>(passesFilter, tItemParam);
            }
        }
    }
    public Expression<Func<TItem, TItemValue>> GetItemValue { get; }
    public Func<TItem, string> GetDisplayedItemValue { get; }
    public Filter<TItemValue>? Filter { get; }
    public string HeaderName { get; }
    public Dictionary<string, object>? CellAttributes { get; }
    public Action<TItem>? OnClick { get; }
    public Order ContentOrder { get; set; }
    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            if (_isHidden == value)
                return;

            _isHidden = value;
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    IFilter? IColumn<TItem>.Filter => Filter;

    public void ToggleContentOrder(bool reverse)
    {
        ContentOrder = ContentOrder == Order.ASK
            ? reverse ? Order.None : Order.DESC
            : ContentOrder == Order.DESC
                ? reverse ? Order.ASK : Order.None
                : reverse ? Order.DESC : Order.ASK;
    }

    public IOrderedQueryable<TItem> OrderByThis(IQueryable<TItem> items)
        => ContentOrder == Order.ASK ? items.OrderBy(GetItemValue) : items.OrderByDescending(GetItemValue);

    public IOrderedQueryable<TItem> ThenByThis(IOrderedQueryable<TItem> items)
        => ContentOrder == Order.ASK ? items.ThenBy(GetItemValue) : items.ThenByDescending(GetItemValue);
}
