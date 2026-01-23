using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Timer = System.Timers.Timer;

namespace Sonorize.Photino.Blazor.Components.Pages.Components;

public partial class LibraryBrowser
{
    [Parameter] public string Title { get; set; } = "Library";
    [Parameter] public string EmptyMessage { get; set; } = "No items found.";
    [Parameter] public List<TItem> Items { get; set; } = [];
    [Parameter] public Func<TItem, string, bool> FilterPredicate { get; set; } = (_, _) => true;

    // View Mode State
    [Parameter] public LibraryViewMode ViewMode { get; set; }
    [Parameter] public EventCallback<LibraryViewMode> ViewModeChanged { get; set; }

    // Templates
    [Parameter] public RenderFragment<TItem> GridTemplate { get; set; } = null!;
    [Parameter] public RenderFragment TableHeader { get; set; } = null!;
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = null!;

    // Events
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    private string _searchQuery = "";
    private List<TItem> _filteredItems = [];
    private Timer _debounceTimer = new(300) { AutoReset = false };

    protected override void OnInitialized()
    {
        _debounceTimer.Elapsed += (s, e) => InvokeAsync(ApplyFilter);
        ApplyFilter();
    }

    protected override void OnParametersSet()
    {
        // Re-apply filter if the source Items list changes reference
        ApplyFilter();
    }

    private void OnSearchKeyUp()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async Task OnViewModeChangedInternal(LibraryViewMode mode)
    {
        await ViewModeChanged.InvokeAsync(mode);
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _filteredItems = Items;
        }
        else
        {
            _filteredItems = Items.Where(x => FilterPredicate(x, _searchQuery)).ToList();
        }
        StateHasChanged();
    }

    private void GoToSettings() => Nav.NavigateTo("/settings");

    public void Dispose()
    {
        _debounceTimer.Dispose();
    }
}