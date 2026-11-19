using Sonorize.Core.Core;
using Sonorize.Core.Library;

namespace Sonorize.Tui;

internal sealed record BrowseDrillDown(string Title, Func<Song, bool> Filter);

/// <summary>A snapshot of the rows and context currently shown by the browser.</summary>
internal sealed record BrowseResults(
    BrowseMode Mode,
    BrowseDrillDown? DrillDown,
    string Query,
    string Title,
    bool LibraryIsEmpty,
    List<Song> Songs,
    List<ArtistGroup> Artists,
    List<AlbumGroup> Albums)
{
    public bool ShowsSongs => DrillDown is not null || Mode is BrowseMode.Songs or BrowseMode.Queue;
    public bool IsQueueView => DrillDown is null && Mode == BrowseMode.Queue;
}
