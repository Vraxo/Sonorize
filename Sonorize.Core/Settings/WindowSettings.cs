namespace Sonorize.Core.Settings;

public class WindowSettings
{
    // Layout State
    public bool IsSidebarOpen { get; set; } = true;
    public int SidebarWidth { get; set; } = 260;

    // Visuals
    public bool ShowSliderThumbs { get; set; } = true;
    public bool EnableSidebarAnimation { get; set; } = true;
    public double ZoomLevel { get; set; } = 1.0;

    // Window Persistence
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
    public int X { get; set; } = -1;
    public int Y { get; set; } = -1;
    public bool IsMaximized { get; set; } = false;
}