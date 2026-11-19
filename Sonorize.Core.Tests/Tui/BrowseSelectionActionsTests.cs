using NSubstitute;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class BrowseSelectionActionsTests
{
    [Fact]
    public void Album_activation_drills_down_by_both_artist_and_title()
    {
        var player = Substitute.For<IPlayerService>();
        var actions = new BrowseSelectionActions(player, new AudioEngineNotifier(() => null));
        var album = new AlbumGroup { Title = "Shared", Artist = "Artist" };
        var results = BrowseResultsBuilder.Build(BrowseMode.Albums, null, "", [], [], [album], []);
        var drill = actions.Activate(results, 0);
        Assert.NotNull(drill);
        Assert.Equal("Shared — Artist", drill.Title);
        Assert.True(drill.Filter(new Song { FilePath = "a", Album = "Shared", Artist = "Artist" }));
        Assert.False(drill.Filter(new Song { FilePath = "b", Album = "Shared", Artist = "Other" }));
        Assert.Null(actions.Activate(results, -1));
        Assert.Null(actions.Activate(results, 1));
    }

    [Fact]
    public void Song_activation_copies_library_context_but_uses_original_queue()
    {
        var player = Substitute.For<IPlayerService>();
        player.IsAudioEngineAvailable.Returns(true);
        var song = new Song { FilePath = "a", Title = "Track" };
        var queue = new List<Song> { song };
        player.PlaybackQueue.Returns(queue);
        var actions = new BrowseSelectionActions(player, new AudioEngineNotifier(() => null));
        var library = BrowseResultsBuilder.Build(BrowseMode.Songs, null, "", [song], [], [], queue);
        actions.Activate(library, 0);
        player.Received(1).PlaySong(song, Arg.Is<List<Song>>(x => x.Count == 1 && x[0] == song && !ReferenceEquals(x, library.Songs)));
        var queueResults = BrowseResultsBuilder.Build(BrowseMode.Queue, null, "", [song], [], [], queue);
        actions.Activate(queueResults, 0);
        player.Received(1).PlaySong(song, queue);
    }

    [Fact]
    public void Removing_filtered_row_resolves_the_underlying_queue_index()
    {
        var player = Substitute.For<IPlayerService>();
        var first = new Song { FilePath = "a", Title = "First" };
        var second = new Song { FilePath = "b", Title = "Second" };
        var queue = new List<Song> { first, second };
        player.PlaybackQueue.Returns(queue);
        var actions = new BrowseSelectionActions(player, new AudioEngineNotifier(() => null));
        var results = BrowseResultsBuilder.Build(BrowseMode.Queue, null, "Second", [], [], [], queue);
        Assert.True(actions.RemoveFromQueue(results, 0));
        player.Received(1).RemoveFromQueue(1);
        Assert.False(actions.RemoveFromQueue(results, 2));
    }
}
