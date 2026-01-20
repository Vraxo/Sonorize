using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared;

public partial class AmbientBackgroundRenderer
{
    private string? _lastSongPath;
    private bool _hasArt;
    private bool _hasCustomBg;

    private string AmbientLayerClass => $"ambient-background-layer {(_hasArt && AppSettings.Theme.EnableAmbientBackground && !_hasCustomBg ? "active" : "")}";

    private string? AmbientLayerStyle
    {
        get => field is not null
        ? $"background-image: url('{field}');"
        : ""; set;
    }

    private string CustomBgStyle
    {
        get
        {
            if (!_hasCustomBg)
            {
                return "";
            }

            string encoded = Uri.EscapeDataString(AppSettings.Theme.BackgroundImagePath ?? "");
            return $"background-image: url('sonorize://localfile/?path={encoded}');";
        }
    }

    protected override void OnInitialized()
    {
        PlayerService.PlaybackStateChanged += OnStateChanged;
        SettingsManager.SettingsSaved += OnStateChanged;
        CheckState();
    }

    private void OnStateChanged()
    {
        _ = InvokeAsync(() =>
        {
            CheckState();
            StateHasChanged();
        });
    }

    private void CheckState()
    {
        string? bgPath = AppSettings.Theme.BackgroundImagePath;
        _hasCustomBg = !string.IsNullOrWhiteSpace(bgPath) && File.Exists(bgPath);

        Song? song = PlayerService.CurrentSong;

        if ((song?.FilePath) == _lastSongPath)
        {
            return;
        }

        _lastSongPath = song?.FilePath;

        if (song is null || !song.HasArt)
        {
            _hasArt = false;
            AmbientLayerStyle = null;
            return;
        }

        string encodedPath = Uri.EscapeDataString(song.FilePath);
        AmbientLayerStyle = $"sonorize://albumart/?path={encodedPath}";
        _hasArt = true;

        return;
    }

    public void Dispose()
    {
        PlayerService.PlaybackStateChanged -= OnStateChanged;
        SettingsManager.SettingsSaved -= OnStateChanged;
    }
}