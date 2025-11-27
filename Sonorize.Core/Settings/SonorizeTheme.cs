namespace Sonorize.Core.Settings;

public class SonorizeTheme
{
    // Colors
    public string AccentColor { get; set; } = "#D9BBFB";
    public string BgPrimary { get; set; } = "#000000";
    public string BgSecondary { get; set; } = "#121212";
    public string BgTertiary { get; set; } = "#242424";
    public string PlayerBarBg { get; set; } = "#181818";
    public string BorderColor { get; set; } = "#282828";
    public string TextPrimary { get; set; } = "#FFFFFF";
    public string TextSecondary { get; set; } = "#B3B3B3";

    // Styling
    public int BorderRadius { get; set; } = 4;
    public bool UsePillButtons { get; set; } = true;
    public bool EnableAmbientBackground { get; set; } = false;
    public bool EnableCustomScrollbars { get; set; } = true;

    // Layout Preferences
    public PlayerBarPosition PlayerBarPosition { get; set; } = PlayerBarPosition.Bottom;
    public SidebarPosition SidebarPosition { get; set; } = SidebarPosition.Left;
    public int PlayerBarHeight { get; set; } = 90; // NEW: Customizable height

    // Fully customizable player bar layout
    public PlayerBarConfig PlayerBarLayout { get; set; } = new();

    // Opacities
    public float SidebarOpacity { get; set; } = 0.9f;
    public float MainContentOpacity { get; set; } = 0.9f;
    public float PlayerBarOpacity { get; set; } = 0.95f;
    public float HighlightOpacity { get; set; } = 0.15f;

    // Background Customization
    public string? BackgroundImagePath { get; set; }
    public int BackgroundBlur { get; set; } = 20;
    public float BackgroundBrightness { get; set; } = 0.4f;
    public int ContentGradientHeight { get; set; } = 250;

    // Typography & Custom
    public string CustomFontFamily { get; set; } = "";
    public int BaseFontSize { get; set; } = 14;
    public string CustomCss { get; set; } = "";

    // Visual Preferences
    public int RowVerticalPadding { get; set; } = 12;
    public int SidebarItemPadding { get; set; } = 10;
    public bool EnableZebraStriping { get; set; } = false;
    public bool ShowGridLines { get; set; } = true;

    public SonorizeTheme Clone()
    {
        var clone = (SonorizeTheme)MemberwiseClone();
        clone.PlayerBarLayout = PlayerBarLayout.Clone();
        return clone;
    }
}