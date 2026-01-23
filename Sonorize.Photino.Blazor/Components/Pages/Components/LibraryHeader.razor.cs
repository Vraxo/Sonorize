using Microsoft.AspNetCore.Components;
using Sonorize.Core;

namespace Sonorize.Photino.Blazor.Components.Pages.Components;

public partial class LibraryHeader
{
    [Parameter] public string Title { get; set; } = "Your Library";
    [Parameter] public bool ShowBackButton { get; set; }
    [Parameter] public EventCallback OnBackClick { get; set; }

    [Parameter] public string SearchQuery { get; set; } = "";
    [Parameter] public EventCallback<string> SearchQueryChanged { get; set; }
    [Parameter] public EventCallback OnSearchKeyUp { get; set; }

    [Parameter] public LibraryViewMode ViewMode { get; set; }
    [Parameter] public EventCallback<LibraryViewMode> ViewModeChanged { get; set; }

    [Parameter] public EventCallback OnSettingsClick { get; set; }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        SearchQuery = e.Value?.ToString() ?? "";
        await SearchQueryChanged.InvokeAsync(SearchQuery);
    }

    private async Task ClearSearch()
    {
        SearchQuery = "";
        await SearchQueryChanged.InvokeAsync(SearchQuery);
        await OnSearchKeyUp.InvokeAsync();
    }

    private async Task ToggleViewMode()
    {
        var newMode = ViewMode == LibraryViewMode.List ? LibraryViewMode.Grid : LibraryViewMode.List;
        await ViewModeChanged.InvokeAsync(newMode);
    }
}