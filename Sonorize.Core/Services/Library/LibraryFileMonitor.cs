using Sonorize.Core.Settings;

namespace Sonorize.Core.Services.Library;

public class LibraryFileMonitor : IDisposable
{
    private readonly SonorizeSettings _settings;
    private readonly List<FileSystemWatcher> _watchers = [];

    public event Action<string>? FileAdded;
    public event Action<string>? FileRemoved;
    public event Action<string, string>? FileRenamed;

    public LibraryFileMonitor(SonorizeSettings settings)
    {
        _settings = settings;
    }

    public void StartWatching()
    {
        StopWatching();

        foreach (string path in _settings.Library.MusicFolderPaths)
        {
            if (!Directory.Exists(path))
            {
                continue;
            }

            try
            {
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.DirectoryName
                };

                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;
                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Monitor] Failed to watch {path}: {ex.Message}");
            }
        }
    }

    private void StopWatching()
    {
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (IsSupported(e.FullPath))
        {
            // Small delay to ensure file handle is released if it was just copied
            _ = Task.Delay(500).ContinueWith(_ => FileAdded?.Invoke(e.FullPath));
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (IsSupported(e.FullPath))
        {
            FileRemoved?.Invoke(e.FullPath);
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (IsSupported(e.FullPath))
        {
            FileRenamed?.Invoke(e.OldFullPath, e.FullPath);
        }
    }

    private bool IsSupported(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return _settings.Library.SupportedFileExtensions.Contains(ext);
    }

    public void Dispose()
    {
        StopWatching();
        GC.SuppressFinalize(this);
    }
}