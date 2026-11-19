using NSubstitute;
using Sonorize.Core.Core;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class BrowseControllerTests
{
    [Fact]
    public void Queue_search_filters_rows_and_removes_the_real_queue_index()
    {
        using var data = new LibraryDataManager(null!, null!, null!, null!);
        var library = new LibraryService(data, null!, null!, null!, null!, null!, new SearchService());
        var player = Substitute.For<IPlayerService>();
        var first = new Song { FilePath = "first.mp3", Title = "First" };
        var second = new Song { FilePath = "second.mp3", Title = "Second" };
        player.PlaybackQueue.Returns(new List<Song> { first, second });
        var layout = new MainWindowLayout();
        try
        {
            var browse = new BrowseController(library, player, layout, new AudioEngineNotifier(() => null));
            browse.SetMode(BrowseMode.Queue);
            layout.Search.Text = "Second";
            Assert.True(browse.IsQueueView);
            Assert.Equal(1, layout.Table.Table!.Rows);
            layout.Table.SetSelection(0, 0, false);
            browse.RemoveSelectedFromQueue();
            player.Received(1).RemoveFromQueue(1);
        }
        finally
        {
            foreach (var view in layout.Views) view.Dispose();
        }
    }
}
