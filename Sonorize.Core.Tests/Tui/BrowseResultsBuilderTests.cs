using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class BrowseResultsBuilderTests
{
    private static readonly Song First = new() { FilePath = "a", Title = "Zebra", Artist = "Artist One", Album = "Alpha" };
    private static readonly Song Second = new() { FilePath = "b", Title = "Apple", Artist = "Artist One", Album = "Alpha" };
    private static readonly Song Third = new() { FilePath = "c", Title = "Zebra", Artist = "Other", Album = "Beta" };
    private static readonly List<Song> Songs = [First, Third, Second];
    private static readonly List<ArtistGroup> Artists = [new() { Name = "Artist One" }, new() { Name = "Other" }];
    private static readonly List<AlbumGroup> Albums = [new() { Title = "Alpha", Artist = "Artist One" }, new() { Title = "Beta", Artist = "Other" }];

    [Fact]
    public void Filters_songs_and_queue_independently()
    {
        var library = BrowseResultsBuilder.Build(BrowseMode.Songs, null, "  zebra artist  ", Songs, Artists, Albums, [Third]);
        Assert.Equal("zebra artist", library.Query);
        Assert.Equal(new[] { First }, library.Songs);
        Assert.Equal("Songs", library.Title);
        Assert.False(library.LibraryIsEmpty);

        var queue = BrowseResultsBuilder.Build(BrowseMode.Queue, null, "zebra", Songs, Artists, Albums, [Third]);
        Assert.Equal(new[] { Third }, queue.Songs);
        Assert.True(queue.IsQueueView);
    }

    [Fact]
    public void Filters_artists_and_albums_by_all_case_insensitive_tokens()
    {
        var artists = BrowseResultsBuilder.Build(BrowseMode.Artists, null, "one artist", Songs, Artists, Albums, []);
        Assert.Equal("Artist One", Assert.Single(artists.Artists).Name);
        var albums = BrowseResultsBuilder.Build(BrowseMode.Albums, null, "one alpha", Songs, Artists, Albums, []);
        Assert.Equal("Alpha", Assert.Single(albums.Albums).Title);
    }

    [Fact]
    public void Drilldown_filters_and_sorts_songs_and_searches_within_them()
    {
        var drill = new BrowseDrillDown("Artist One", song => song.Artist == "Artist One");
        var all = BrowseResultsBuilder.Build(BrowseMode.Artists, drill, "", Songs, Artists, Albums, []);
        Assert.True(all.ShowsSongs);
        Assert.False(all.IsQueueView);
        Assert.Equal("Artist One", all.Title);
        Assert.Equal(new[] { Second, First }, all.Songs);
        var filtered = BrowseResultsBuilder.Build(BrowseMode.Artists, drill, "zebra", Songs, Artists, Albums, []);
        Assert.Equal(new[] { First }, filtered.Songs);
    }
}
