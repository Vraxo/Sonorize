using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;

namespace Sonorize.Tui;

/// <summary>Builds a browse snapshot from the library, queue, navigation state and search query.</summary>
internal sealed class BrowseResultsBuilder
{
    private readonly LibraryService _library;
    private readonly IPlayerService _player;

    public BrowseResultsBuilder(LibraryService library, IPlayerService player)
    {
        _library = library;
        _player = player;
    }

    public BrowseResults Build(BrowseMode mode, BrowseDrillDown? drillDown, string query) =>
        Build(mode, drillDown, query, _library.AllSongs, _library.AllArtists, _library.AllAlbums,
            drillDown is null && mode == BrowseMode.Queue ? _player.PlaybackQueue.ToArray() : []);

    internal static BrowseResults Build(BrowseMode mode, BrowseDrillDown? drillDown, string query,
        IReadOnlyList<Song> songs, IReadOnlyList<ArtistGroup> artists, IReadOnlyList<AlbumGroup> albums,
        IReadOnlyList<Song> queue)
    {
        query = query.Trim();
        List<Song> songRows = [];
        List<ArtistGroup> artistRows = [];
        List<AlbumGroup> albumRows = [];
        string title;

        if (drillDown is not null)
        {
            var matchingSongs = songs.Where(drillDown.Filter)
                .OrderBy(s => s.Album, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase);
            songRows = [.. SearchService.Search(matchingSongs, query)];
            title = drillDown.Title;
        }
        else
        {
            switch (mode)
            {
                case BrowseMode.Artists:
                    artistRows = [.. artists.Where(a => Matches(query, a.Name))];
                    title = "Artists";
                    break;
                case BrowseMode.Albums:
                    albumRows = [.. albums.Where(a => Matches(query, a.Title, a.Artist))];
                    title = "Albums";
                    break;
                case BrowseMode.Queue:
                    songRows = [.. SearchService.Search(queue, query)];
                    title = "Queue";
                    break;
                default:
                    songRows = [.. SearchService.Search(songs, query)];
                    title = "Songs";
                    break;
            }
        }

        return new BrowseResults(mode, drillDown, query, title, songs.Count == 0, songRows, artistRows, albumRows);
    }

    private static bool Matches(string query, params string?[] fields)
    {
        if (query.Length == 0)
            return true;

        string[] tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.All(token => fields.Any(f => f?.Contains(token, StringComparison.OrdinalIgnoreCase) == true));
    }
}
