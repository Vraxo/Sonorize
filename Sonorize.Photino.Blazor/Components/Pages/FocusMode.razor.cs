using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class FocusMode
{
    private string? _artSrc;
    private Sonorize.Core.Models.Song? _currentSong;
    private bool _isBackgroundBright = false;

    private string ContainerClass => $"focus-container {(_isBackgroundBright ? "adaptive-dark-text" : "")}";

    protected override void OnInitialized()
    {
        PlayerService.PlaybackStateChanged += OnStateChanged;
        PlayerService.QueueChanged += OnStateChanged;
        SettingsManager.SettingsSaved += OnStateChanged;

        UpdateState();
    }

    private async void OnStateChanged()
    {
        await InvokeAsync(async () =>
        {
            await UpdateState();
            StateHasChanged();
        });
    }

    private async Task UpdateState()
    {
        Song? song = PlayerService.CurrentSong;

        if (song == _currentSong && _currentSong is not null)
        {
            return; // Debounce if song hasn't changed
        }

        _currentSong = song;

        if (_currentSong is null || !_currentSong.HasArt)
        {
            _artSrc = null;
            _isBackgroundBright = false;
            return;
        }

        string encodedPath = Uri.EscapeDataString(_currentSong.FilePath);
        _artSrc = $"sonorize://albumart/?path={encodedPath}";

        // Analyze brightness using C# Service
        // We use the raw FilePath, as the service reads the file directly
        _isBackgroundBright = await ImageAnalysis.IsAlbumArtBrightAsync(_currentSong.FilePath);
    }

    public void Dispose()
    {
        PlayerService.PlaybackStateChanged -= OnStateChanged;
        PlayerService.QueueChanged -= OnStateChanged;
        SettingsManager.SettingsSaved -= OnStateChanged;
    }
}