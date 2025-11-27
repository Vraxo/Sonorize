using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Sonorize.Core.Settings;
using System.Drawing;

namespace Sonorize.Photino.Blazor.Setup;

public class WindowStateManager
{
    private readonly PhotinoBlazorApp _app;
    private readonly SonorizeSettings _settings;
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly object _saveLock = new();
    private CancellationTokenSource? _debounceCts;

    // Minimum size required to render the Player Bar fully without overlap
    private const int MinWidth = 960;
    private const int MinHeight = 600;

    public WindowStateManager(PhotinoBlazorApp app)
    {
        _app = app;
        _settings = app.Services.GetRequiredService<SonorizeSettings>();
        _settingsManager = app.Services.GetRequiredService<ISettingsManager<SonorizeSettings>>();
    }

    public void Initialize()
    {
        // Resolve absolute path to ensure Photino can find the icon regardless of working directory.
        // This is critical for the Taskbar to pick up the runtime window icon correctly.
        string iconPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "img", "icon.ico");

        _ = _app.MainWindow
            .SetTitle("Sonorize")
            .SetIconFile(iconPath)
            .SetLogVerbosity(0);

        // Enforce the minimum size at the OS level
        // This prevents the user from shrinking the window to a point where the UI breaks
        _ = _app.MainWindow.SetMinSize(MinWidth, MinHeight);

        RestoreState();
        RestoreZoom();
        HookEvents();
    }

    private void RestoreState()
    {
        // Ensure stored size is at least the minimum
        int targetWidth = Math.Max(_settings.Window.Width, MinWidth);
        int targetHeight = Math.Max(_settings.Window.Height, MinHeight);

        bool isValidPos = _settings.Window.X > -10000 && _settings.Window.Y > -10000;

        _ = _app.MainWindow.SetSize(targetWidth, targetHeight);

        _ = isValidPos
            ? _app.MainWindow.SetLocation(new Point(_settings.Window.X, _settings.Window.Y))
            : _app.MainWindow.Center();
    }

    private void RestoreZoom()
    {
        // Photino uses integer percentage (100 = 1.0)
        int zoomPercent = (int)(_settings.Window.ZoomLevel * 100);
        // Safety clamp
        zoomPercent = Math.Clamp(zoomPercent, 30, 300);
        _ = _app.MainWindow.SetZoom(zoomPercent);
    }

    private void HookEvents()
    {
        // Listen for settings changes to apply zoom dynamically
        _settingsManager.SettingsSaved += OnSettingsSaved;

        _app.MainWindow.WindowLocationChanged += (sender, location) =>
        {
            if (location.X > -10000 && location.Y > -10000)
            {
                _settings.Window.X = location.X;
                _settings.Window.Y = location.Y;
                TriggerDebouncedSave();
            }
        };

        _app.MainWindow.WindowSizeChanged += (sender, size) =>
        {
            // Even though SetMinSize enforces it, we double-check before saving to avoid bad data
            if (size.Width >= MinWidth && size.Height >= MinHeight)
            {
                _settings.Window.Width = size.Width;
                _settings.Window.Height = size.Height;
                TriggerDebouncedSave();
            }
        };

        _app.MainWindow.WindowClosing += (sender, e) =>
        {
            SaveImmediately();
            return false; // Allow close
        };
    }

    private void OnSettingsSaved()
    {
        // Apply zoom whenever settings are saved (e.g. from shortcuts or settings menu)
        RestoreZoom();
    }

    private void TriggerDebouncedSave()
    {
        lock (_saveLock)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            CancellationToken token = _debounceCts.Token;

            _ = Task.Delay(500, token).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }

                try { _settingsManager.Save(_settings); } catch { }
            });
        }
    }

    private void SaveImmediately()
    {
        // Final validation before close
        if (_app.MainWindow.Location.X > -10000 && _app.MainWindow.Location.Y > -10000)
        {
            _settings.Window.X = _app.MainWindow.Location.X;
            _settings.Window.Y = _app.MainWindow.Location.Y;
        }

        if (_app.MainWindow.Size.Width >= MinWidth && _app.MainWindow.Size.Height >= MinHeight)
        {
            _settings.Window.Width = _app.MainWindow.Size.Width;
            _settings.Window.Height = _app.MainWindow.Size.Height;
        }

        _settingsManager.Save(_settings);
    }
}