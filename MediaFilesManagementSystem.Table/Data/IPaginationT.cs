namespace MediaFilesManagementSystem.Table.Data;

public interface IPagination<TItem> : IPagination
{
    IEnumerable<TItem> CurrentPageItems { get; }
    IQueryable<TItem> AllPagesItems { get; set; }

    void CollectionChanged();
}
