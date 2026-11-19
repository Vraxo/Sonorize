using NSubstitute;
using Sonorize.Core.Settings;
using Sonorize.Tui;

namespace Sonorize.Core.Tests.Tui;

public class MusicFolderServiceTests
{
    [Fact]
    public void Add_existing_folder_saves_and_scans_once_and_rejects_duplicate()
    {
        var settings = new SonorizeSettings();
        var manager = Substitute.For<ISettingsManager<SonorizeSettings>>();
        var scanned = new List<string>();
        var service = new MusicFolderService(settings, manager, scanned.Add);
        string folder = Path.GetTempPath();

        Assert.Equal(AddFolderResult.Added, service.Add($" {folder} ", out string path));
        Assert.Equal(Path.GetFullPath(folder), path);
        Assert.Equal(new[] { path }, settings.Library.MusicFolderPaths);
        Assert.Equal(new[] { path }, scanned);
        manager.Received(1).Save(settings);

        Assert.Equal(AddFolderResult.AlreadyAdded, service.Add(folder, out _));
        Assert.Single(scanned);
        manager.Received(1).Save(settings);
    }

    [Fact]
    public void Add_missing_folder_does_not_save_or_scan()
    {
        var settings = new SonorizeSettings();
        var manager = Substitute.For<ISettingsManager<SonorizeSettings>>();
        var scanned = new List<string>();
        var service = new MusicFolderService(settings, manager, scanned.Add);
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Assert.Equal(AddFolderResult.NotFound, service.Add(folder, out string path));
        Assert.Equal(Path.GetFullPath(folder), path);
        Assert.Empty(settings.Library.MusicFolderPaths);
        Assert.Empty(scanned);
        manager.DidNotReceive().Save(Arg.Any<SonorizeSettings>());
    }
}
