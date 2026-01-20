using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Shared;

public partial class MainLayout
{
    private const int AnimationDurationMs = 320;
    private const long MaxFileSize = 1_000_000_000; // 1GB

    private bool _isDragging = false;
    private int _dragCounter = 0;
    private bool _showEq = false;
    private bool _showSpeedPitch = false;
    private DotNetObjectReference<MainLayout>? _objRef;

    private string MainContainerClass
    {
        get
        {
            List<string> classes = ["layout-root"];

            if (AppSettings.Theme.EnableAmbientBackground)
            {
                classes.Add("ambient-mode");
            }

            string barPos = AppSettings.Theme.PlayerBarPosition.ToString().ToLowerInvariant();
            classes.Add($"bar-pos-{barPos}");

            return string.Join(" ", classes);
        }
    }

    private string MiddleContainerClass =>
        $"middle-container sidebar-pos-{AppSettings.Theme.SidebarPosition.ToString().ToLowerInvariant()}";

    private string MainContentClass => $"main-content {(AppSettings.Window.EnableSidebarAnimation
        ? ""
        : "no-anim")}";

    private string MainContentStyle => (AppSettings.Window.IsSidebarOpen, AppSettings.Theme.SidebarPosition) switch
    {
        (true, _) => string.Empty,
        (false, SidebarPosition.Left) => "padding-left: 40px;",
        (false, SidebarPosition.Right) => "padding-right: 40px;",
        _ => string.Empty
    };

    protected override void OnInitialized()
    {
        SettingsManager.SettingsSaved += OnSettingsChanged;

        LayoutState.EqRequested += OpenEqualizer;
        LayoutState.SpeedPitchRequested += OpenSpeedPitch;
        LayoutState.FocusModeRequested += ToggleFocus;
        LayoutState.QueueViewRequested += ToggleQueue;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }
        try
        {
            _objRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("registerZoomHandler", _objRef);
        }
        catch { /* Ignore JS init errors */ }
    }

    [JSInvokable]
    public void AdjustZoom(int direction)
    {
        if (direction == 0)
        {
            AppSettings.Window.ZoomLevel = 1.0;
        }
        else
        {
            double step = 0.05;
            double newVal = AppSettings.Window.ZoomLevel + (direction * step);
            AppSettings.Window.ZoomLevel = Math.Clamp(newVal, 0.5, 1.5);
        }

        SettingsManager.Save(AppSettings);
    }

    private void OnSettingsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task ToggleSidebar()
    {
        if (AppSettings.Window.EnableSidebarAnimation)
        {
            await JS.InvokeVoidAsync("startLayoutLock", true);
        }

        AppSettings.Window.IsSidebarOpen = !AppSettings.Window.IsSidebarOpen;
        SettingsManager.Save(AppSettings);

        if (!AppSettings.Window.EnableSidebarAnimation)
        {
            return;
        }

        await Task.Delay(AnimationDurationMs);
        await JS.InvokeVoidAsync("startLayoutLock", false);
    }

    private void OpenEqualizer()
    {
        _showEq = true;
        StateHasChanged();
    }

    private void CloseEqualizer()
    {
        _showEq = false;
        StateHasChanged();
    }

    private void OpenSpeedPitch()
    {
        _showSpeedPitch = true;
        StateHasChanged();
    }

    private void CloseSpeedPitch()
    {
        _showSpeedPitch = false;
        StateHasChanged();
    }

    private void ToggleFocus()
    {
        Nav.NavigateTo(Nav.Uri.EndsWith("focus") ? "/" : "/focus");
    }

    private void ToggleQueue()
    {
        Nav.NavigateTo(Nav.Uri.EndsWith("queue") ? "/" : "/queue");
    }

    private void OnDragEnter(DragEventArgs e)
    {
        if (LayoutState.IsDraggingInternal)
        {
            return;
        }

        _dragCounter++;

        if (_dragCounter == 1)
        {
            _isDragging = true;
        }
    }

    private void OnDragLeave(DragEventArgs e)
    {
        if (LayoutState.IsDraggingInternal)
        {
            return;
        }

        _dragCounter--;

        if (_dragCounter > 0)
        {
            return;
        }

        _dragCounter = 0;
        _isDragging = false;
    }

    private async Task HandleFileDrop(InputFileChangeEventArgs e)
    {
        _dragCounter = 0;
        _isDragging = false;

        IReadOnlyList<IBrowserFile> browserFiles = e.GetMultipleFiles(500);

        if (browserFiles.Count == 0)
        {
            return;
        }

        List<(Stream, string Name)> fileStreams = [.. browserFiles.Select(f => (f.OpenReadStream(MaxFileSize), f.Name))];

        try
        {
            List<Song> songs = await FileImporter.ImportStreamsAsync(fileStreams);

            if (songs.Count <= 0)
            {
                return;
            }

            await PlayerService.PlayPlaylist(songs);
        }
        finally
        {
            foreach ((Stream? stream, string _) in fileStreams)
            {
                stream.Dispose();
            }
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();

        SettingsManager.SettingsSaved -= OnSettingsChanged;
        LayoutState.EqRequested -= OpenEqualizer;
        LayoutState.SpeedPitchRequested -= OpenSpeedPitch;
        LayoutState.FocusModeRequested -= ToggleFocus;
        LayoutState.QueueViewRequested -= ToggleQueue;

        GC.SuppressFinalize(this);
    }
}