using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Sonorize.Core.Settings;
using Sonorize.Tui;
using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Sonorize.Core.Tests.Tui;

public class ShortcutControllerTests
{
    [Fact]
    public void Playback_shortcuts_and_cover_toggle_are_dispatched()
    {
        using var data = new LibraryDataManager(null!, null!, null!, null!);
        var library = new LibraryService(data, null!, null!, null!, null!, null!, new SearchService());
        var player = Substitute.For<IPlayerService>();
        player.TotalTime.Returns(TimeSpan.FromSeconds(100));
        player.CurrentTime.Returns(TimeSpan.FromSeconds(20));
        var settings = new SonorizeSettings();
        var manager = Substitute.For<ISettingsManager<SonorizeSettings>>();
        var layout = new MainWindowLayout();
        using var window = new Runnable();
        try
        {
            var notifier = new AudioEngineNotifier(() => null);
            var scans = new LibraryScanRunner(library, settings, NullLogger<LibraryScanRunner>.Instance, () => { });
            var browse = new BrowseController(library, player, layout, notifier);
            var cover = new CoverArtController(player, layout);
            var folders = new MusicFolderService(settings, manager, _ => { });
            var dialog = new MusicFolderDialog(window, folders);
            var shortcuts = new ShortcutController(player, library, scans, browse, cover, dialog, notifier, layout, () => null);

            Assert.True(shortcuts.OnKeyDown(Key.N));
            player.Received(1).PlayNext();
            Assert.True(shortcuts.OnKeyDown(Key.CursorRight));
            player.Received(1).Seek(0.25);
            Assert.True(shortcuts.OnKeyDown(Key.C));
            Assert.False(layout.Cover.Visible);
            Assert.True(shortcuts.OnKeyDown(Key.C));
            Assert.True(layout.Cover.Visible);
        }
        finally
        {
            foreach (var view in layout.Views) view.Dispose();
        }
    }
}
