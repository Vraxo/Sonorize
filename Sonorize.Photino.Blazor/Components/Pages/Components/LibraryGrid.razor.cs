using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages.Components;

public partial class LibraryGrid
{
    [Parameter] public List<Song> Songs { get; set; } = [];
    [Parameter] public EventCallback<Song> OnSongClick { get; set; }

    private ElementReference _container;
    private DotNetObjectReference<LibraryGrid>? _objRef;
    private List<List<Song>> _chunkedSongs = [];
    private int _columns = 4;
    private float _estimatedRowHeight = 250;
    private double _lastKnownWidth = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _objRef = DotNetObjectReference.Create(this);
            try
            {
                await JS.InvokeVoidAsync("observeResize", _container, _objRef);
            }
            catch { /* Ignore JS errors during init/disposal race */ }
        }
    }

    protected override void OnParametersSet()
    {
        // If we haven't received a width yet, we might use a fallback or wait.
        // If we have a width, re-calc.
        if (Songs.Count <= 250)
        {
            return;
        }

        if (_lastKnownWidth > 0)
        {
            RecalculateLayout(_lastKnownWidth);
        }
        else
        {
            // Fallback initial calculation
            double width = AppSettings.Window.Width;
            if (AppSettings.Window.IsSidebarOpen)
            {
                width -= AppSettings.Window.SidebarWidth;
            }

            RecalculateLayout(width);
        }
    }

    [JSInvokable]
    public void OnContainerResize(double width)
    {
        if (double.Abs(_lastKnownWidth - width) <= 5) // Debounce small sub-pixel jitters
        {
            return;
        }
        _lastKnownWidth = width;

        if (Songs.Count <= 250)
        {
            return;
        }

        RecalculateLayout(width);
        StateHasChanged();
    }

    private void RecalculateLayout(double containerWidth)
    {
        int itemWidth = AppSettings.Library.GridItemWidth;
        int gap = AppSettings.Library.GridGap;

        // Calculate columns based on actual container width
        // We use Math.Floor to ensure they fit
        _columns = Math.Max(1, (int)Math.Floor((containerWidth + gap) / (itemWidth + gap)));

        float verticalPadding = AppSettings.Library.GridItemPadding * 2;
        float textAreaHeight = 65; // Fixed allowance for 2 lines of text
        _estimatedRowHeight = (float)Math.Ceiling(itemWidth + verticalPadding + textAreaHeight + gap);

        ChunkData();
    }

    private void ChunkData()
    {
        _chunkedSongs = [.. Songs
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / _columns)
            .Select(x => x.Select(v => v.Value).ToList())];
    }

    private string GetArtUrl(Song song)
    {
        return $"sonorize://albumart/?path={Uri.EscapeDataString(song.FilePath)}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("unobserveResize", _container);
        }
        catch { }

        _objRef?.Dispose();
    }
}