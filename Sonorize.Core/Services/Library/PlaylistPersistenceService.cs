using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using System.Text.Json;

namespace Sonorize.Core.Services.Library;

public class PlaylistPersistenceService
{
    private readonly string _playlistDir;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public PlaylistPersistenceService()
        : this(AppDataHelper.GetSubDirectory("Playlists"))
    {
    }

    // Internal constructor for testing
    internal PlaylistPersistenceService(string baseDir)
    {
        _playlistDir = baseDir;

        if (!Directory.Exists(_playlistDir))
        {
            _ = Directory.CreateDirectory(_playlistDir);
        }
    }

    public List<Playlist> LoadPlaylists()
    {
        var list = new List<Playlist>();

        if (!Directory.Exists(_playlistDir))
        {
            return list;
        }

        foreach (string file in Directory.EnumerateFiles(_playlistDir, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var playlist = JsonSerializer.Deserialize<Playlist>(json);
                if (playlist is not null)
                {
                    // Ensure the type is forced to Manual for these files
                    playlist.Type = PlaylistType.Manual;
                    // Ensure the ID matches the filename if possible, or just trust the file content
                    list.Add(playlist);
                }
            }
            catch { /* Ignore corrupt files */ }
        }

        return list.OrderBy(p => p.Name).ToList();
    }

    public void SavePlaylist(Playlist playlist)
    {
        // Use ID as filename to allow renaming without file moves
        string path = Path.Combine(_playlistDir, $"{playlist.Id}.json");
        string json = JsonSerializer.Serialize(playlist, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public void DeletePlaylist(Playlist playlist)
    {
        string path = Path.Combine(_playlistDir, $"{playlist.Id}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}