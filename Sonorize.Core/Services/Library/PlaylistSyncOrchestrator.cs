using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class PlaylistSyncOrchestrator
{
    public enum SyncMode
    {
        Full,
        Incremental
    }

    public static List<Playlist> Sync(List<Playlist> existing, List<Playlist> found, SyncMode mode)
    {
        return mode == SyncMode.Full
            ? PerformFullSync(existing, found)
            : PerformIncrementalSync(existing, found);
    }

    private static List<Playlist> PerformFullSync(List<Playlist> existing, List<Playlist> found)
    {
        Dictionary<string, Playlist> existingMap = existing
            .Where(p => !string.IsNullOrEmpty(p.FilePath))
            .ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);

        List<Playlist> newList = new(found.Count);

        foreach (Playlist? playlist in found.Where(p => !string.IsNullOrEmpty(p.FilePath)))
        {
            if (existingMap.TryGetValue(playlist.FilePath, out Playlist? existingPlaylist))
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

    private static List<Playlist> PerformIncrementalSync(List<Playlist> existing, List<Playlist> found)
    {
        Dictionary<string, Playlist> map = existing
            .Where(p => !string.IsNullOrEmpty(p.FilePath))
            .ToDictionary(p => p.FilePath!, StringComparer.OrdinalIgnoreCase);

        bool changed = false;

        foreach (Playlist? playlist in found.Where(p => !string.IsNullOrEmpty(p.FilePath)))
        {
            if (map.TryGetValue(playlist.FilePath, out Playlist? existingPlaylist))
            {
                UpdatePlaylist(existingPlaylist, playlist);
            }
            else
            {
                existing.Add(playlist);
                changed = true;
            }
        }

        return changed || found.Count > 0
            ? existing
            : existing;
    }

    private static void UpdatePlaylist(Playlist target, Playlist source)
    {
        target.Name = source.Name;
        target.SongFilePaths = source.SongFilePaths;
    }
}