using NativeFileDialogs.Net;
using Sonorize.Core.Helpers;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Components.Pages.Components;

public partial class ThemeColorsCard
{
    private readonly SonorizeTheme _defaults = new();
    private readonly ActionDebouncer _debouncer = new();

    // --- Proxy Properties for Immediate Saving ---

    private int BackgroundBlur
    {
        get => AppSettings.Theme.BackgroundBlur;

        set
        {
            if (AppSettings.Theme.BackgroundBlur == value)
            {
                return;
            }

            AppSettings.Theme.BackgroundBlur = value;
            Save();
        }
    }

    private string AccentColor
    {
        get => AppSettings.Theme.AccentColor;

        set
        {
            if (AppSettings.Theme.AccentColor == value)
            {
                return;
            }

            AppSettings.Theme.AccentColor = value;
            Save();
        }
    }

    private string BgPrimary
    {
        get => AppSettings.Theme.BgPrimary;

        set
        {
            if (AppSettings.Theme.BgPrimary == value)
            {
                return;
            }

            AppSettings.Theme.BgPrimary = value;
            Save();
        }
    }

    private string BgSecondary
    {
        get => AppSettings.Theme.BgSecondary;

        set
        {
            if (AppSettings.Theme.BgSecondary == value)
            {
                return;
            }

            AppSettings.Theme.BgSecondary = value; Save();
        }
    }

    private string BgTertiary
    {
        get => AppSettings.Theme.BgTertiary;

        set
        {
            if (AppSettings.Theme.BgTertiary == value)
            {
                return;
            }

            AppSettings.Theme.BgTertiary = value; Save();
        }
    }

    private string PlayerBarBg
    {
        get => AppSettings.Theme.PlayerBarBg;

        set
        {
            if (AppSettings.Theme.PlayerBarBg == value)
            {
                return;
            }

            AppSettings.Theme.PlayerBarBg = value;
            Save();
        }
    }

    private string BorderColor
    {
        get => AppSettings.Theme.BorderColor;

        set
        {
            if (AppSettings.Theme.BorderColor == value)
            {
                return;
            }

            AppSettings.Theme.BorderColor = value;
            Save();
        }
    }

    private string TextPrimary
    {
        get => AppSettings.Theme.TextPrimary;

        set
        {
            if (AppSettings.Theme.TextPrimary == value)
            {
                return;
            }

            AppSettings.Theme.TextPrimary = value;
            Save();
        }
    }

    private string TextSecondary
    {
        get => AppSettings.Theme.TextSecondary;

        set
        {
            if (AppSettings.Theme.TextSecondary == value)
            {
                return;
            }

            AppSettings.Theme.TextSecondary = value;
            Save();
        }
    }

    // --- Existing Proxies ---

    private int SidebarOpacityInt
    {
        get => (int)(AppSettings.Theme.SidebarOpacity * 100);

        set
        {
            AppSettings.Theme.SidebarOpacity = float.Clamp(value / 100f, 0.05f, 1.0f);
            Save();
        }
    }

    private int MainContentOpacityInt
    {
        get => (int)(AppSettings.Theme.MainContentOpacity * 100);

        set
        {
            AppSettings.Theme.MainContentOpacity = float.Clamp(value / 100f, 0.05f, 1.0f);
            Save();
        }
    }

    private int PlayerBarOpacityInt
    {
        get => (int)(AppSettings.Theme.PlayerBarOpacity * 100);

        set
        {
            AppSettings.Theme.PlayerBarOpacity = float.Clamp(value / 100f, 0.05f, 1.0f);
            Save();
        }
    }

    private int BgBrightnessInt
    {
        get => (int)(AppSettings.Theme.BackgroundBrightness * 100);

        set
        {
            AppSettings.Theme.BackgroundBrightness = float.Clamp(value / 100f, 0.0f, 1.0f);
            Save();
        }
    }

    private int GradientHeightInt
    {
        get => AppSettings.Theme.ContentGradientHeight;

        set
        {
            AppSettings.Theme.ContentGradientHeight = value;
            Save();
        }
    }

    private void PickBackgroundImage()
    {
        Dictionary<string, string> filters = new()
        {
            { "Images", "jpg,jpeg,png,gif,webp" }
        };

        if (Nfd.OpenDialog(out string? path, filters) != NfdStatus.Ok || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        AppSettings.Theme.BackgroundImagePath = path;

        Save(immediate: true);
    }

    private void ClearBackgroundImage()
    {
        AppSettings.Theme.BackgroundImagePath = null;

        Save(immediate: true);
    }

    private void Reset(Action<SonorizeSettings> updateAction)
    {
        updateAction(AppSettings);
        Save(immediate: true);
    }

    private void Save(bool immediate = false)
    {
        if (immediate)
        {
            // Cancel pending debounce to avoid overwriting immediate save
            _debouncer.Debounce(() => { }, 0);
            SettingsManager.Save(AppSettings);
            return;
        }

        _debouncer.Debounce(() =>
        {
            try
            {
                SettingsManager.Save(AppSettings);
            }
            catch { }
        }, 500);
    }

    public void Dispose()
    {
        _debouncer.Dispose();
    }
}