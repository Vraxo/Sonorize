namespace Sonorize.Core.Settings;

public class ThemeLayout
{
    // Styling
    public int BorderRadius { get; set; } = 8;
    public bool UsePillButtons { get; set; } = true;
    public bool EnableAmbientBackground { get; set; } = true;
    public bool EnableCustomScrollbars { get; set; } = true;

    // Layout Preferences
    public PlayerBarPosition PlayerBarPosition { get; set; } = PlayerBarPosition.Bottom;
    public SidebarPosition SidebarPosition { get; set; } = SidebarPosition.Left;
    public int PlayerBarHeight { get; set; } = 90;

    // Fully customizable player bar layout
    public PlayerBarConfig PlayerBarLayout { get; set; } = new();

    // Opacities
    public float SidebarOpacity { get; set; } = 0.9f;
    public float MainContentOpacity { get; set; } = 0.85f;
    public float PlayerBarOpacity { get; set; } = 0.95f;
    public float HighlightOpacity { get; set; } = 0.15f;

    // Background Customization
    public string? BackgroundImagePath { get; set; }
    public int BackgroundBlur { get; set; } = 50;
    public float BackgroundBrightness { get; set; } = 0.3f;
    public int ContentGradientHeight { get; set; } = 300;

    // Typography & Custom
    public string CustomFontFamily { get; set; } = "";
    public int BaseFontSize { get; set; } = 14;
    public string CustomCss { get; set; } = "";

    // Visual Preferences
    public int RowVerticalPadding { get; set; } = 12;
    public int SidebarItemPadding { get; set; } = 10;
    public bool EnableZebraStriping { get; set; } = false;
    public bool ShowGridLines { get; set; } = false;

    public ThemeLayout Clone()
    {
        var clone = (ThemeLayout)MemberwiseClone();
        clone.PlayerBarLayout = PlayerBarLayout.Clone();
        return clone;
    }
}