namespace MediaFilesManagementSystem.Table.Data;

public class ColumnVisibilityChangedEventArgs<TItem> : EventArgs
{
    public IColumn<TItem> Column { get; set; }
}
