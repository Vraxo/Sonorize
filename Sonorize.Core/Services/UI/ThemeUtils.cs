using Sonorize.Core.Settings;
using System.Globalization;
using System.Text;

namespace Sonorize.Core.Services.UI;

public static class ThemeUtils
{
    public static string Lighten(string hexColor, float factor)
    {
        return AdjustBrightness(hexColor, factor);
    }

    public static string Darken(string hexColor, float factor)
    {
        return AdjustBrightness(hexColor, -factor);
    }

    public static string GenerateRootCss(SonorizeTheme theme, bool useTransparency)
    {
        StringBuilder sb = new();
        _ = sb.AppendLine(":root {");

        _ = sb.AppendLine($"--accent-primary: {theme.AccentColor};");
        _ = sb.AppendLine($"--accent-primary-hover: {Lighten(theme.AccentColor, 0.1f)};");

        string bgPrimary = useTransparency ? HexToRgba(theme.BgPrimary, theme.SidebarOpacity) : theme.BgPrimary;
        _ = sb.AppendLine($"--bg-primary: {bgPrimary};");

        string bgSec = useTransparency ? HexToRgba(theme.BgSecondary, theme.MainContentOpacity) : theme.BgSecondary;
        _ = sb.AppendLine($"--bg-secondary: {bgSec};");

        string bgTer = useTransparency ? HexToRgba(theme.BgTertiary, theme.MainContentOpacity) : theme.BgTertiary;
        _ = sb.AppendLine($"--bg-tertiary: {bgTer};");

        string playerBg = useTransparency ? HexToRgba(theme.PlayerBarBg, theme.PlayerBarOpacity) : theme.PlayerBarBg;
        _ = sb.AppendLine($"--player-bar-bg: {playerBg};");

        string hoverBg = HexToRgba(GetSmartHighlightBase(theme.TextPrimary, theme.HighlightOpacity), theme.HighlightOpacity);
        _ = sb.AppendLine($"--hover-bg: {hoverBg};");

        // Force opaque background for modals to ensure readability
        _ = sb.AppendLine($"--modal-bg: {theme.BgTertiary};");

        _ = sb.AppendLine($"--text-primary: {theme.TextPrimary};");
        _ = sb.AppendLine($"--text-secondary: {theme.TextSecondary};");

        string border = useTransparency ? "rgba(255,255,255,0.1)" : theme.BorderColor;
        _ = sb.AppendLine($"--border-color: {border};");

        _ = sb.AppendLine($"--scrollbar-thumb: {Lighten(theme.BgSecondary, 0.2f)};");

        _ = sb.AppendLine($"--border-radius: {theme.BorderRadius}px;");
        string btnRadius = theme.UsePillButtons ? "500px" : $"{theme.BorderRadius}px";
        _ = sb.AppendLine($"--btn-radius: {btnRadius};");

        _ = sb.AppendLine($"--base-font-size: {theme.BaseFontSize}px;");

        // Spacing variables
        _ = sb.AppendLine($"--row-padding: {theme.RowVerticalPadding}px;");
        _ = sb.AppendLine($"--sidebar-item-padding: {theme.SidebarItemPadding}px;");

        // NEW: Player Bar Height
        _ = sb.AppendLine($"--player-bar-height: {theme.PlayerBarHeight}px;");

        // Background Filters
        _ = sb.AppendLine($"--bg-blur: {theme.BackgroundBlur}px;");
        _ = sb.AppendLine($"--bg-brightness: {theme.BackgroundBrightness.ToString("0.##", CultureInfo.InvariantCulture)};");

        // Main Content Background (Gradient vs Flat)
        string mainBg = theme.ContentGradientHeight > 0
            ? $"linear-gradient(to bottom, var(--bg-tertiary) 0%, var(--bg-secondary) {theme.ContentGradientHeight}px)"
            : "var(--bg-secondary)";
        _ = sb.AppendLine($"--main-content-bg: {mainBg};");

        if (!string.IsNullOrWhiteSpace(theme.CustomFontFamily))
        {
            _ = sb.Append("--font-family: '");
            _ = sb.Append(theme.CustomFontFamily);
            _ = sb.AppendLine("', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;");
        }

        _ = sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GetSmartHighlightBase(string hexColor, float opacity)
    {
        if (opacity <= 0.5f)
        {
            return hexColor;
        }

        if (TryGetRgb(hexColor, out int r, out int g, out int b))
        {
            double luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
            return luminance > 128 ? "#000000" : "#FFFFFF";
        }
        return "#000000";
    }

    public static string HexToRgba(string hex, float alpha)
    {
        return TryGetRgb(hex, out int r, out int g, out int b)
            ? $"rgba({r}, {g}, {b}, {alpha.ToString(CultureInfo.InvariantCulture)})"
            : $"rgba(0, 0, 0, {alpha})";
    }

    private static string AdjustBrightness(string hex, float factor)
    {
        if (!TryGetRgb(hex, out int r, out int g, out int b))
        {
            return "#000000";
        }

        if (factor < 0)
        {
            factor = 1 + factor;
            r = (int)(r * factor);
            g = (int)(g * factor);
            b = (int)(b * factor);
        }
        else
        {
            r = (int)(((255 - r) * factor) + r);
            g = (int)(((255 - g) * factor) + g);
            b = (int)(((255 - b) * factor) + b);
        }

        return $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";
    }

    private static bool TryGetRgb(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrEmpty(hex))
        {
            return false;
        }

        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Join("", hex.Select(c => new string(c, 2)));
        }

        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, null, out int val))
        {
            return false;
        }

        r = (val >> 16) & 0xFF;
        g = (val >> 8) & 0xFF;
        b = val & 0xFF;
        return true;
    }
}