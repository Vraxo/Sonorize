using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Terminal.Gui.ViewBase;

namespace Sonorize.Tui;

public sealed partial class MainWindow
{
    // "999:59 / 999:59"
    private const int TimeLabelWidth = 15;

    private string? _markedSongPath;
    private float _shownFraction = -1f;

    private void UpdateNowPlaying()
    {
        Song? song = _player.CurrentSong;

        SetText(_nowPlaying, song is null
            ? "Nothing playing"
            : $"{(_player.IsPlaying ? "▶" : "‖")} {song.Title} — {song.Artist} · {song.Album}");

        TimeSpan current = _player.CurrentTime;
        TimeSpan total = _player.TotalTime;

        float fraction = total > TimeSpan.Zero
            ? (float)Math.Clamp(current / total, 0d, 1d)
            : 0f;

        if (Math.Abs(fraction - _shownFraction) > 0.0005f)
        {
            _shownFraction = fraction;
            _progress.Fraction = fraction;
        }

        SetText(_time, $"{FormatTime(current)} / {FormatTime(total)}");
        SetText(_status, BuildStatus());

        // The "▶" marker in the table is computed at draw time, so a redraw is
        // all that is needed when the current song changes.
        if (song?.FilePath != _markedSongPath)
        {
            _markedSongPath = song?.FilePath;
            _table.SetNeedsDraw();
        }
    }

    private string BuildStatus()
    {
        if (!_player.IsAudioEngineAvailable)
        {
            return "Audio engine unavailable: the BASS native library was not found (see README)";
        }

        string repeat = _player.RepeatMode switch
        {
            RepeatMode.All => "all",
            RepeatMode.One => "one",
            _ => "off"
        };

        string status = $"Volume {Math.Round(_player.Volume * 100)}%   Shuffle {(_player.IsShuffle ? "on" : "off")}   Repeat {repeat}";

        return Volatile.Read(ref _activeScans) > 0
            ? status + "   Scanning library…"
            : status;
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes}:{time.Seconds:d2}";
    }

    /// <summary>Assigns only on change, so the 200 ms tick does not force redraws.</summary>
    private static void SetText(View view, string text)
    {
        if (view.Text != text)
        {
            view.Text = text;
        }
    }
}
