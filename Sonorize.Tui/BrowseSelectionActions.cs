using Sonorize.Core.Core;
using Sonorize.Core.Playback;

namespace Sonorize.Tui;

/// <summary>Resolves row activation and queue-removal actions against a browse snapshot.</summary>
internal sealed class BrowseSelectionActions
{
    private readonly IPlayerService _player;
    private readonly AudioEngineNotifier _audioNotifier;

    public BrowseSelectionActions(IPlayerService player, AudioEngineNotifier audioNotifier)
    {
        _player = player;
        _audioNotifier = audioNotifier;
    }

    // A non-null result opens a drill-down; song activation starts playback instead.
    public BrowseDrillDown? Activate(BrowseResults results, int row)
    {
        if (row < 0)
            return null;

        if (results.ShowsSongs)
        {
            if (row < results.Songs.Count)
                Play(results.Songs[row], results);
            return null;
        }

        if (results.Mode == BrowseMode.Artists && row < results.Artists.Count)
        {
            string artist = results.Artists[row].Name;
            return new BrowseDrillDown(BrowseText.Cell(artist), s => s.Artist == artist);
        }
        if (results.Mode == BrowseMode.Albums && row < results.Albums.Count)
        {
            var album = results.Albums[row];
            return new BrowseDrillDown($"{BrowseText.Cell(album.Title)} — {BrowseText.Cell(album.Artist)}",
                s => s.Album == album.Title && s.Artist == album.Artist);
        }
        return null;
    }

    private void Play(Song song, BrowseResults results)
    {
        if (!_player.IsAudioEngineAvailable)
        {
            _audioNotifier.ShowUnavailable();
            return;
        }

        // A queue selection uses the existing queue; library results become a new queue.
        List<Song> context = results.IsQueueView ? _player.PlaybackQueue : [.. results.Songs];
        _ = _player.PlaySong(song, context);
    }

    public bool RemoveFromQueue(BrowseResults results, int row)
    {
        if (row < 0 || row >= results.Songs.Count)
            return false;

        // Resolve against the real queue because the visible rows may be filtered.
        int queueIndex = _player.PlaybackQueue.IndexOf(results.Songs[row]);
        if (queueIndex < 0)
            return false;

        _player.RemoveFromQueue(queueIndex);
        return true;
    }

}
