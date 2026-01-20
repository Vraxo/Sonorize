using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Shared;

public partial class Sidebar
{
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public bool IsOpen { get; set; } = true;

    private bool _isResizing = false;
    private double _startX;
    private int _startWidth;

    private string SidebarClass
    {
        get
        {
            List<string> classes =
            [
                "sidebar",
                IsOpen ? "open" : "closed"
            ];

            if (AppSettings.Window.EnableSidebarAnimation && !_isResizing)
            {
                classes.Add("animated");
            }

            return string.Join(" ", classes);
        }
    }

    private string SidebarStyle => IsOpen
        ? $"width: {AppSettings.Window.SidebarWidth}px;"
        : "width: 0px; padding: 0; border: none;";

    protected override void OnInitialized()
    {
        Nav.LocationChanged += OnLocationChanged;
    }

    private void StartResize(MouseEventArgs e)
    {
        _isResizing = true;
        _startX = e.ClientX;
        _startWidth = AppSettings.Window.SidebarWidth;
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        double delta = e.ClientX - _startX;

        if (AppSettings.Theme.SidebarPosition == SidebarPosition.Right)
        {
            delta = -delta;
        }

        int newWidth = int.Clamp((int)(_startWidth + delta), 150, 600);

        if (AppSettings.Window.SidebarWidth == newWidth)
        {
            return;
        }

        AppSettings.Window.SidebarWidth = newWidth;
    }

    private void StopResize(MouseEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        _isResizing = false;
        SettingsManager.Save(AppSettings);
    }

    private void HandleMouseOut(MouseEventArgs e)
    {
        if (!_isResizing || e.Buttons != 0)
        {
            return;
        }

        StopResize(e);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void NavigateTo(string url)
    {
        Nav.NavigateTo(url);
    }

    private string GetActiveState(string href)
    {
        string currentUri = Nav.Uri;
        string targetUri = Nav.ToAbsoluteUri(href).ToString();

        bool isActive = (href == "albums" && currentUri.Contains("/albums")) ||
                        (href == "artists" && currentUri.Contains("/artists")) ||
                        (href == "playlists" && currentUri.EndsWith("/playlists")) ||
                        currentUri.Equals(targetUri, StringComparison.OrdinalIgnoreCase);

        return isActive ? "active" : "";
    }

    public void Dispose()
    {
        Nav.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}