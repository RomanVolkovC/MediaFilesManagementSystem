namespace MediaFilesManagementSystem.Table.Data;

public class TableFacade<TItem> : IDisposable
{
    private readonly IQueryable<TItem> _items;
    private readonly IColumn<TItem>[] _columns;
    private readonly ColumnsContentOrderManager<TItem> _columnsContentOrderManager = new();
    private readonly ColumnsFiltersManager<TItem> _columnsFiltersManager = new();
    private readonly Pagination<TItem> _pagination;

    public TableFacade(IQueryable<TItem> items, IColumn<TItem>[] columns, int pageSize)
    {
        if (columns is { Length: < 1 or > 1000 })
            throw new ArgumentException("Количество колонок должно быть больше 0 и меньше 1001.", nameof(columns));

        _items = items;
        _columns = columns;
        _pagination = new(pageSize, items);

        foreach (var column in columns)
        {
            column.VisibilityChanged += Column_VisibilityChanged;

            if (column.Filter != null)
                column.Filter.ValidConditionSetted += Filter_ValidConditionSetted;
        }
    }

    public event EventHandler<ColumnVisibilityChangedEventArgs<TItem>>? ColumnVisibilityChanged;

    public IReadOnlyList<IColumn<TItem>> Columns => _columns;
    public IPagination<TItem> Pagination => _pagination;

    public void ChangeMainColumnContentOrder(IColumn<TItem> column, bool reverse)
    {
        if (!_columns.Contains(column))
            throw new ArgumentException("Данная колонка отсутствует в списке колонок.", nameof(column));

        _columnsContentOrderManager.ChangeMainColumnContentOrder(column, reverse);
        _pagination.AllPagesItems = _columnsContentOrderManager.OrderItems(_columnsFiltersManager.FilterItems(_items));
    }

    public void ChangeAdditionalColumnContentOrder(IColumn<TItem> column, bool reverse)
    {
        if (!_columns.Contains(column))
            throw new ArgumentException("Данная колонка отсутствует в списке колонок.", nameof(column));

        _columnsContentOrderManager.ChangeAdditionalColumnContentOrder(column, reverse);
        _pagination.AllPagesItems = _columnsContentOrderManager.OrderItems(_columnsFiltersManager.FilterItems(_items));
    }

    public void Dispose()
    {
        foreach (var column in _columns)
        {
            column.VisibilityChanged -= Column_VisibilityChanged;

            if (column.Filter != null)
                column.Filter.ValidConditionSetted -= Filter_ValidConditionSetted;
        }

        _pagination.Dispose();
    }

    private void Column_VisibilityChanged(object? sender, EventArgs e)
        => ColumnVisibilityChanged?.Invoke(this, new() { Column = (IColumn<TItem>)(sender ?? throw new ArgumentNullException(nameof(sender))) });

    private void Filter_ValidConditionSetted(object? sender, EventArgs e)
    {
        _columnsFiltersManager.ApplyFiltersConditions(_columns);
        _pagination.AllPagesItems = _columnsContentOrderManager.OrderItems(_columnsFiltersManager.FilterItems(_items));
    }
}
