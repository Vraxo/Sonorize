using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace Sonorize.Tui;

public sealed partial class MainWindow
{
    // "999:59 / 999:59"
    private const int TimeLabelWidth = 15;

    // "Shuffle off" and "Repeat off", the longest texts of each label.
    private const int ShuffleLabelWidth = 11;
    private const int RepeatLabelWidth = 10;
    private const int StatusGap = 3;

    private string? _markedSongPath;
    private float _shownFraction = -1f;
    private bool? _statusIsError;
    private Scheme? _nowPlayingScheme;
    private bool? _shuffleShown;
    private RepeatMode? _repeatShown;

    private void UpdateNowPlaying()
    {
        Song? song = _player.CurrentSong;

        SetText(_nowPlaying, song is null
            ? "Nothing playing"
            : $"{(_player.IsPlaying ? "▶" : "‖")} {song.Title} — {song.Artist} · {song.Album}");

        Scheme nowPlayingScheme = song is null
            ? TuiColors.Muted
            : _player.IsPlaying ? TuiColors.Playing : TuiColors.Notice;

        if (!ReferenceEquals(nowPlayingScheme, _nowPlayingScheme))
        {
            _nowPlayingScheme = nowPlayingScheme;
            _nowPlaying.SetScheme(nowPlayingScheme);
        }

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

        bool statusIsError = !_player.IsAudioEngineAvailable;

        if (statusIsError != _statusIsError)
        {
            _statusIsError = statusIsError;
            _status.SetScheme(statusIsError ? TuiColors.Error : TuiColors.Muted);

            // The error message takes the whole line.
            _shuffle.Visible = !statusIsError;
            _repeat.Visible = !statusIsError;
            _status.X = statusIsError ? 1 : Pos.Right(_repeat) + StatusGap;
        }

        UpdateShuffleAndRepeat();

        // The "▶" marker in the table is computed at draw time, so a redraw is
        // all that is needed when the current song changes.
        if (song?.FilePath != _markedSongPath)
        {
            _markedSongPath = song?.FilePath;
            _table.SetNeedsDraw();
        }
    }

    /// <summary>Shuffle and repeat are dim while off and yellow while on.</summary>
    private void UpdateShuffleAndRepeat()
    {
        bool shuffle = _player.IsShuffle;

        if (shuffle != _shuffleShown)
        {
            _shuffleShown = shuffle;
            _shuffle.Text = shuffle ? "Shuffle on" : "Shuffle off";
            _shuffle.SetScheme(shuffle ? TuiColors.Notice : TuiColors.Muted);
        }

        RepeatMode repeat = _player.RepeatMode;

        if (repeat != _repeatShown)
        {
            _repeatShown = repeat;

            _repeat.Text = repeat switch
            {
                RepeatMode.All => "Repeat all",
                RepeatMode.One => "Repeat one",
                _ => "Repeat off"
            };

            _repeat.SetScheme(repeat == RepeatMode.None ? TuiColors.Muted : TuiColors.Notice);
        }
    }

    private string BuildStatus()
    {
        if (!_player.IsAudioEngineAvailable)
        {
            return "Audio engine unavailable: the BASS native library was not found (see README)";
        }

        string status = $"Volume {Math.Round(_player.Volume * 100)}%";

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
