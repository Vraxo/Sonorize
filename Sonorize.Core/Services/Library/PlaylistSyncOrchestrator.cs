using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class PlaylistSyncOrchestrator
{
    public enum SyncMode { Full, Incremental }

    public List<Playlist> Sync(List<Playlist> existing, List<Playlist> found, SyncMode mode)
    {
        return mode == SyncMode.Full
            ? PerformFullSync(existing, found)
            : PerformIncrementalSync(existing, found);
    }

    private List<Playlist> PerformFullSync(List<Playlist> existing, List<Playlist> found)
    {
        var existingMap = existing
            .Where(p => !string.IsNullOrEmpty(p.FilePath))
            .ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);

        var newList = new List<Playlist>(found.Count);

        foreach (var playlist in found.Where(p => !string.IsNullOrEmpty(p.FilePath)))
        {
            if (existingMap.TryGetValue(playlist.FilePath, out var existingPlaylist))
            {
                UpdatePlaylist(existingPlaylist, playlist);
                newList.Add(existingPlaylist);
            }
            else
            {
                newList.Add(playlist);
            }
        }

        return newList;
    }

    private List<Playlist> PerformIncrementalSync(List<Playlist> existing, List<Playlist> found)
    {
        var map = existing
            .Where(p => !string.IsNullOrEmpty(p.FilePath))
            .ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);

        bool changed = false;

        foreach (var playlist in found.Where(p => !string.IsNullOrEmpty(p.FilePath)))
        {
            if (map.TryGetValue(playlist.FilePath, out var existingPlaylist))
            {
                UpdatePlaylist(existingPlaylist, playlist);
            }
            else
            {
                existing.Add(playlist);
                changed = true;
            }
        }

        return changed || found.Count > 0 ? existing : existing;
    }

    private void UpdatePlaylist(Playlist target, Playlist source)
    {
        target.Name = source.Name;
        target.SongFilePaths = source.SongFilePaths;
    }
}