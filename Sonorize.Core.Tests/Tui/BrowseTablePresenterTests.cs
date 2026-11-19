using NSubstitute;
using Sonorize.Core.Core;
using Sonorize.Core.Playback;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class BrowseTablePresenterTests
{
    [Fact]
    public void Presents_results_and_empty_hints()
    {
        var player = Substitute.For<IPlayerService>();
        var layout = new MainWindowLayout();
        try
        {
            var presenter = new BrowseTablePresenter(layout.Table, layout.EmptyHint, player);
            var empty = BrowseResultsBuilder.Build(BrowseMode.Songs, null, "", [], [], [], []);
            presenter.Present(empty, false);
            Assert.Equal("Songs (0)", layout.Table.Title);
            Assert.True(layout.EmptyHint.Visible);
            Assert.Contains("library is empty", layout.EmptyHint.Text);

            var noMatches = BrowseResultsBuilder.Build(BrowseMode.Queue, null, "missing", [], [], [], []);
            presenter.Present(noMatches, false);
            Assert.Equal("No matches.", layout.EmptyHint.Text);

            var song = new Song { FilePath = "a", Title = "Track" };
            var rows = BrowseResultsBuilder.Build(BrowseMode.Songs, null, "", [song], [], [], []);
            presenter.Present(rows, false);
            Assert.Equal("Songs (1)", layout.Table.Title);
            Assert.False(layout.EmptyHint.Visible);
            Assert.Equal(1, layout.Table.Table!.Rows);
        }
        finally
        {
            foreach (var view in layout.Views) view.Dispose();
        }
    }
}
