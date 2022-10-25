
namespace MediaFilesManagementSystem.Table.Data
{
    public interface IPagination
    {
        event EventHandler? CurrentPageItemsChanging;
        event EventHandler? CurrentPageItemsChanged;
        event EventHandler? StateChanged;

        int CurrentPageNumber { get; set; }
        bool HasNextPage { get; }
        bool HasPreviousPage { get; }
        int LastPageNumber { get; }
        int PageSize { get; set; }
    }
}