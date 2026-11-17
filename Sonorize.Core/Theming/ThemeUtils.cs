using System.Globalization;
using System.Text;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Theming;

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

        // Player bar sizing
        _ = sb.AppendLine($"--player-bar-height: {theme.PlayerBarHeight}px;");

        // Background filters
        _ = sb.AppendLine($"--bg-blur: {theme.BackgroundBlur}px;");
        _ = sb.AppendLine($"--bg-brightness: {theme.BackgroundBrightness.ToString("0.##", CultureInfo.InvariantCulture)};");

        // Main content background (gradient vs flat)
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

        AppendDesignTokens(sb);

        _ = sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendDesignTokens(StringBuilder sb)
    {
        // Semantic color aliases let components use intent-based names while
        // keeping the existing theme variables available for compatibility.
        _ = sb.AppendLine("--color-accent: var(--accent-primary);");
        _ = sb.AppendLine("--color-accent-hover: var(--accent-primary-hover);");
        _ = sb.AppendLine("--color-background: var(--bg-primary);");
        _ = sb.AppendLine("--color-surface: var(--bg-secondary);");
        _ = sb.AppendLine("--color-surface-raised: var(--bg-tertiary);");
        _ = sb.AppendLine("--color-player-bar: var(--player-bar-bg);");
        _ = sb.AppendLine("--color-border: var(--border-color);");
        _ = sb.AppendLine("--color-text-primary: var(--text-primary);");
        _ = sb.AppendLine("--color-text-secondary: var(--text-secondary);");
        _ = sb.AppendLine("--color-hover: var(--hover-bg);");
        _ = sb.AppendLine("--color-control-track: #535353;");
        _ = sb.AppendLine("--color-modal: var(--modal-bg);");
        _ = sb.AppendLine("--color-backdrop: rgba(0, 0, 0, 0.7);");

        // A shared 4px-based spacing scale, with aliases for the existing
        // per-component spacing settings.
        _ = sb.AppendLine("--space-1: 4px;");
        _ = sb.AppendLine("--space-2: 8px;");
        _ = sb.AppendLine("--space-3: 12px;");
        _ = sb.AppendLine("--space-4: 16px;");
        _ = sb.AppendLine("--space-5: 24px;");
        _ = sb.AppendLine("--space-6: 32px;");
        _ = sb.AppendLine("--space-7: 40px;");
        _ = sb.AppendLine("--space-8: 48px;");
        _ = sb.AppendLine("--space-row-y: var(--row-padding);");
        _ = sb.AppendLine("--space-sidebar-item: var(--sidebar-item-padding);");

        // Shape tokens preserve the current radius controls while giving
        // surfaces and overlays a consistent starting scale.
        _ = sb.AppendLine("--radius-control: var(--border-radius);");
        _ = sb.AppendLine("--radius-button: var(--btn-radius);");
        _ = sb.AppendLine("--radius-card: calc(var(--border-radius) * 2);");
        _ = sb.AppendLine("--radius-overlay: calc(var(--border-radius) * 3);");
        _ = sb.AppendLine("--radius-pill: 999px;");

        // Typography tokens scale with the user's selected base font size.
        _ = sb.AppendLine("--font-family-ui: var(--font-family, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif);");
        _ = sb.AppendLine("--font-size-caption: calc(var(--base-font-size) * 0.75);");
        _ = sb.AppendLine("--font-size-small: calc(var(--base-font-size) * 0.875);");
        _ = sb.AppendLine("--font-size-body: var(--base-font-size);");
        _ = sb.AppendLine("--font-size-subheading: calc(var(--base-font-size) * 1.125);");
        _ = sb.AppendLine("--font-size-heading: calc(var(--base-font-size) * 1.25);");
        _ = sb.AppendLine("--font-size-display: calc(var(--base-font-size) * 1.5);");
        _ = sb.AppendLine("--line-height-tight: 1.2;");
        _ = sb.AppendLine("--line-height-normal: 1.5;");
        _ = sb.AppendLine("--line-height-relaxed: 1.65;");

        // Interaction tokens establish shared defaults for future component
        // migrations without changing existing focus behavior.
        _ = sb.AppendLine("--motion-duration-fast: 120ms;");
        _ = sb.AppendLine("--motion-duration-standard: 180ms;");
        _ = sb.AppendLine("--motion-duration-slow: 260ms;");
        _ = sb.AppendLine("--motion-easing-standard: cubic-bezier(0.2, 0, 0, 1);");
        _ = sb.AppendLine("--focus-ring: 0 0 0 2px var(--bg-primary), 0 0 0 4px var(--accent-primary);");
        _ = sb.AppendLine("--shadow-subtle: 0 1px 2px rgb(0 0 0 / 0.16);");
        _ = sb.AppendLine("--shadow-raised: 0 8px 24px rgb(0 0 0 / 0.22);");
        _ = sb.AppendLine("--shadow-control-thumb: 0 2px 4px rgba(0, 0, 0, 0.3);");
        _ = sb.AppendLine("--shadow-modal: 0 20px 50px rgba(0, 0, 0, 0.7);");
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
