using Sonorize.Core.Core;
using Sonorize.Core.Playlists;

namespace Sonorize.Core.Library;

public class DemoDataLoader
{
    private readonly LibraryDataManager _dataManager;

    public DemoDataLoader(LibraryDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public void Load()
    {
        _dataManager.ClearData();

        List<Song> songs = DemoDataGenerator.Generate(100);

        foreach (Song song in songs)
        {
            _dataManager.AddOrUpdateSong(song);
        }

        List<Playlist> playlists = DemoDataGenerator.GeneratePlaylists(songs);

        _dataManager.UpdateFilePlaylists(playlists, incremental: false);
    }
}
