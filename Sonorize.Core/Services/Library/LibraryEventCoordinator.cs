using Sonorize.Core.Models;
using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class LibraryEventCoordinator : IDisposable
{
    private readonly SonorizeSettings _settings;
    private readonly LibraryDataManager _dataManager;
    private readonly LibraryFileMonitor _fileMonitor;

    public LibraryEventCoordinator(
        SonorizeSettings settings,
        LibraryDataManager dataManager,
        LibraryFileMonitor fileMonitor)
    {
        _settings = settings;
        _dataManager = dataManager;
        _fileMonitor = fileMonitor;

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _fileMonitor.FileAdded += OnFileAdded;
        _fileMonitor.FileRemoved += OnFileRemoved;
        _fileMonitor.FileRenamed += OnFileRenamed;
    }

    public void StartFileMonitoring()
    {
        _fileMonitor.StartWatching();
    }

    private void OnFileAdded(string path)
    {
        if (_dataManager.GetSong(path) != null)
        {
            return;
        }

        MusicLibraryService musicLibrary = new();
        Song? song = musicLibrary.CreateSongFromFileAsync(path).GetAwaiter().GetResult();

        if (song == null)
        {
            return;
        }

        _dataManager.AddOrUpdateSong(song);
    }

    private void OnFileRemoved(string path)
    {
        _dataManager.RemoveSong(path);
    }

    private void OnFileRenamed(string oldPath, string newPath)
    {
        if (_dataManager.GetSong(oldPath) is not Song song)
        {
            OnFileAdded(newPath);
            return;
        }

        song.FilePath = newPath;
        _dataManager.RemoveSong(oldPath);
        _dataManager.AddOrUpdateSong(song);
    }

    public void Dispose()
    {
        _fileMonitor.FileAdded -= OnFileAdded;
        _fileMonitor.FileRemoved -= OnFileRemoved;
        _fileMonitor.FileRenamed -= OnFileRenamed;
    }
}