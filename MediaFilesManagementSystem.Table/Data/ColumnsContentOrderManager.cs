namespace MediaFilesManagementSystem.Table.Data;

internal class ColumnsContentOrderManager<T>
{
    private readonly List<IColumn<T>> _columnsOrder = new();

    public IQueryable<T> OrderItems(IQueryable<T> items)
    {
        IOrderedQueryable<T>? orderedItems = _columnsOrder.FirstOrDefault()?.OrderByThis(items);

        if (orderedItems != null)
            foreach (var column in _columnsOrder.Skip(1))
                orderedItems = column.ThenByThis(orderedItems);

        return orderedItems ?? items;
    }

    public void ChangeMainColumnContentOrder(IColumn<T> column, bool reverse)
    {
        var firstColumn = _columnsOrder.FirstOrDefault();

        if (firstColumn == null)
        {
            AddColumnToContentOrderList(column, reverse);
        }
        else if (firstColumn == column)
        {
            ToggleFirstColumnContentOrder(firstColumn, reverse);
        }
        else
        {
            ClearColumnsContentOrder();
            AddColumnToContentOrderList(column, reverse);
        }
    }

    public void ChangeAdditionalColumnContentOrder(IColumn<T> column, bool reverse)
    {
        var firstColumn = _columnsOrder.FirstOrDefault();

        if (column == firstColumn)
            ToggleFirstColumnContentOrder(firstColumn, reverse);
        else if (_columnsOrder.Contains(column))
            ToggleColumnContentOrder(column, reverse);
        else
            AddColumnToContentOrderList(column, reverse);
    }

    private void ToggleFirstColumnContentOrder(IColumn<T> firstColumn, bool reverse)
    {
        ToggleColumnContentOrder(firstColumn, reverse);

        var newFirstColumn = _columnsOrder.FirstOrDefault();
        if (newFirstColumn != null && newFirstColumn != firstColumn)
            ClearColumnsContentOrder();
    }

    private void ToggleColumnContentOrder(IColumn<T> column, bool reverse)
    {
        column.ToggleContentOrder(reverse);

        if (column.ContentOrder == Order.None)
            if (!_columnsOrder.Remove(column))
                throw new ArgumentException("Ошибка удаления элемента из коллекции.", nameof(column));
    }

    private void AddColumnToContentOrderList(IColumn<T> column, bool reverse)
    {
        column.ContentOrder = reverse ? Order.DESC : Order.ASK;
        _columnsOrder.Add(column);
    }

    private void ClearColumnsContentOrder()
    {
        foreach (var column in _columnsOrder)
            column.ContentOrder = Order.None;
        _columnsOrder.Clear();
    }
}
