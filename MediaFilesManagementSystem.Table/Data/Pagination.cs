using Microsoft.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;

namespace MediaFilesManagementSystem.Table.Data;

public class Pagination<TItem> : IPagination<TItem>, IDisposable
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private IQueryable<TItem> _allPagesItems;
    private int _currentPageNumber = 1;
    private int _pageSize;
    private bool _updating;

    public Pagination(int pageSize, IQueryable<TItem> allPagesItems)
    {
        _pageSize = pageSize;
        _allPagesItems = allPagesItems;
        _ = UpdateLastPage();
        CurrentPageItems = GetCurrentPageItems().ToList();
    }

    public event EventHandler? CurrentPageItemsChanging;
    public event EventHandler? CurrentPageItemsChanged;
    public event EventHandler? StateChanged;

    public IEnumerable<TItem> CurrentPageItems { get; private set; }
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }
    public int LastPageNumber { get; private set; }

    [MemberNotNull(nameof(_allPagesItems))]
    public IQueryable<TItem> AllPagesItems
    {
        get => _allPagesItems;
        set
        {
            _allPagesItems = value;

            if (UpdateLastPage())
                StateChanged?.Invoke(this, EventArgs.Empty);

            UpdateCurrentPageItems();
        }
    }
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value == _pageSize)
                return;

            _pageSize = value < 1 ? 1 : value;

            if (UpdateLastPage())
                StateChanged?.Invoke(this, EventArgs.Empty);

            UpdateCurrentPageItems();
        }
    }
    public int CurrentPageNumber
    {
        get => _currentPageNumber;
        set
        {
            if (value == _currentPageNumber)
                return;

            if (value > LastPageNumber || value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            _currentPageNumber = value;
            UdpatePageState();
            StateChanged?.Invoke(this, EventArgs.Empty);
            UpdateCurrentPageItems();
        }
    }

    public void CollectionChanged()
    {
        if (UpdateLastPage())
            StateChanged?.Invoke(this, EventArgs.Empty);

        UpdateCurrentPageItems();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    private IQueryable<TItem> GetCurrentPageItems() => AllPagesItems.Skip(PageSize * (CurrentPageNumber - 1)).Take(PageSize);

    private async void UpdateCurrentPageItems()
    {
        if (_updating)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new();
        }
        else
        {
            _updating = true;
            CurrentPageItemsChanging?.Invoke(this, EventArgs.Empty);
        }

        var token = _cancellationTokenSource.Token;

        try
        {
            var items = await GetCurrentPageItems().ToListAsync(token);
            token.ThrowIfCancellationRequested();
            CurrentPageItems = items;
            CurrentPageItemsChanged?.Invoke(this, EventArgs.Empty);
            _updating = false;
        }
        catch (OperationCanceledException) { }
    }

    private bool UpdateLastPage()
    {
        int newValue = (AllPagesItems.Count() + PageSize - 1) / PageSize;

        if (newValue < 1)
            newValue = 1;

        if (LastPageNumber != newValue)
        {
            LastPageNumber = newValue;

            if (_currentPageNumber > newValue)
                _currentPageNumber = newValue;

            UdpatePageState();

            return true;
        }
        return false;
    }

    private void UdpatePageState()
    {
        HasPreviousPage = CurrentPageNumber > 1;
        HasNextPage = CurrentPageNumber < LastPageNumber;
    }
}
