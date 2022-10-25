using MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

using System.Linq.Expressions;

namespace MediaFilesManagementSystem.Table.Data;

public interface IColumn<TItem>
{
    event EventHandler? VisibilityChanged;

    Expression<Func<TItem, bool>> PassesFilter { get; }
    Func<TItem, string> GetDisplayedItemValue { get; }
    IFilter? Filter { get; }
    string HeaderName { get; }
    Dictionary<string, object>? CellAttributes { get; }
    Action<TItem>? OnClick { get; }
    Order ContentOrder { get; set; }
    bool IsHidden { get; set; }

    void ToggleContentOrder(bool reverse);
    IOrderedQueryable<TItem> OrderByThis(IQueryable<TItem> items);
    IOrderedQueryable<TItem> ThenByThis(IOrderedQueryable<TItem> items);
}
