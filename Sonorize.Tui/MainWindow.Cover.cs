using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace Sonorize.Tui;

public sealed partial class MainWindow
{
    private sealed record CoverResult(string? SongPath, Color[,]? Pixels);

    private bool _showCover = true;

    // In 16-colour mode a cover drawn as character cells is an unreadable
    // mosaic, so the panel hides itself unless the terminal can draw pixels.
    // Detection is asynchronous, hence the short delay; pressing c overrides it.
    private const int RasterCheckTick = 5;
    private int _coverTicks;
    private bool _coverToggledByUser;

    // Path of the song whose cover is shown or being loaded.
    private string? _coverSongPath;
    private bool _coverLoaded;

    // Written by the loader task, consumed by the UI tick.
    private CoverResult? _pendingCover;

    /// <summary>Called from the UI tick: starts a load on song change and applies finished ones.</summary>
    private void UpdateCover()
    {
        if (!_coverToggledByUser && _showCover && ++_coverTicks == RasterCheckTick && !_cover.IsUsingRasterGraphics)
        {
            SetCoverVisible(false);
        }

        if (!_showCover)
        {
            return;
        }

        string? songPath = _player.CurrentSong?.FilePath;

        if (songPath != _coverSongPath || !_coverLoaded)
        {
            _coverSongPath = songPath;
            _coverLoaded = true;

            if (songPath is null)
            {
                Interlocked.Exchange(ref _pendingCover, new CoverResult(null, null));
            }
            else
            {
                // Tag parsing and image decoding are too slow for the UI thread.
                _ = Task.Run(() => Interlocked.Exchange(ref _pendingCover, new CoverResult(songPath, AlbumArtLoader.Load(songPath))));
            }
        }

        CoverResult? finished = Interlocked.Exchange(ref _pendingCover, null);

        // Ignore a result that arrived after the song changed again.
        if (finished is not null && finished.SongPath == _coverSongPath)
        {
            _cover.Image = finished.Pixels;
            _cover.SetNeedsDraw();
        }
    }

    private void ToggleCover()
    {
        _coverToggledByUser = true;
        SetCoverVisible(!_showCover);
    }

    private void SetCoverVisible(bool visible)
    {
        _showCover = visible;
        _cover.Visible = _showCover;

        // Without the cover the sidebar takes the full column.
        _sidebar.Height = _showCover ? Dim.Absolute(SidebarListHeight) : Dim.Fill(FooterHeight);

        if (_showCover)
        {
            _coverLoaded = false;
        }
        else
        {
            _cover.Image = null;
        }
    }
}
