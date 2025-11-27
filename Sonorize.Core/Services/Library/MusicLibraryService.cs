using Sonorize.Core.Helpers;
using Sonorize.Core.Models;
using System.Collections.Concurrent;

namespace Sonorize.Core.Services.Library;

public class MusicLibraryService : IMusicLibraryService
{
    private static readonly HashSet<string> PlaylistExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".m3u", ".m3u8"
    };

    public Task<List<Song>> LoadSongsFromFolderAsync(string folderPath, IEnumerable<string> extensions, CancellationToken cancellationToken)
    {
        // Create a fast lookup set from the provided extensions
        HashSet<string> extensionSet = new(extensions, StringComparer.OrdinalIgnoreCase);

        return Task.Run(() =>
        {
            // Use shared safe walker
            IEnumerable<string> musicFiles = FileSystemHelper.GetFilesSafe(folderPath, extensionSet);

            // Thread-safe collection for parallel processing
            ConcurrentBag<Song> songs = [];

            try
            {
                _ = Parallel.ForEach(musicFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, (file) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Pre-check for validity before heavy parsing
                    if (IsFileValid(file))
                    {
                        songs.Add(ProcessMusicFile(file));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return [];
            }

            return songs.ToList();

        }, cancellationToken);
    }

    public Task<List<Playlist>> LoadPlaylistsFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            IEnumerable<string> playlistFiles = FileSystemHelper.GetFilesSafe(folderPath, PlaylistExtensions);
            List<Playlist> playlists = [];

            foreach (string file in playlistFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (IsFileValid(file))
                {
                    Playlist? playlist = ProcessPlaylistFile(file);
                    if (playlist != null)
                    {
                        playlists.Add(playlist);
                    }
                }
            }

            return playlists;
        }, cancellationToken);
    }

    public Task<Song?> CreateSongFromFileAsync(string filePath)
    {
        return Task.Run(() =>
        {
            return !IsFileValid(filePath) ? null : ProcessMusicFile(filePath);
        });
    }

    // --- Metadata Editing ---

    public SongMetadata? GetMetadata(string filePath)
    {
        if (!IsFileValid(filePath))
        {
            return null;
        }

        try
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            return new SongMetadata
            {
                Title = tag.Title ?? "",
                Artist = tag.FirstPerformer ?? "",
                Album = tag.Album ?? "",
                AlbumArtists = tag.FirstAlbumArtist ?? "",
                Genre = tag.FirstGenre ?? "",
                Year = tag.Year,
                Track = tag.Track,
                Disc = tag.Disc
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to read metadata for {filePath}: {ex.Message}");
            return null;
        }
    }

    public bool SaveMetadata(string filePath, SongMetadata metadata)
    {
        if (!IsFileValid(filePath))
        {
            return false;
        }

        try
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            tag.Title = metadata.Title;
            tag.Performers = string.IsNullOrWhiteSpace(metadata.Artist) ? [] : [metadata.Artist];
            tag.Album = metadata.Album;
            tag.AlbumArtists = string.IsNullOrWhiteSpace(metadata.AlbumArtists) ? [] : [metadata.AlbumArtists];
            tag.Genres = string.IsNullOrWhiteSpace(metadata.Genre) ? [] : [metadata.Genre];
            tag.Year = metadata.Year;
            tag.Track = metadata.Track;
            tag.Disc = metadata.Disc;

            file.Save();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to save metadata for {filePath}: {ex.Message}");
            return false;
        }
    }

    // --- Helper ---

    private static bool IsFileValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            // Guard against 0-byte files which cause crashes in many parsers
            return new FileInfo(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    // --- Internal Processors ---

    private static Song ProcessMusicFile(string filePath)
    {
        try
        {
            using TagLib.File tagFile = TagLib.File.Create(filePath);
            return CreateSongFromTags(tagFile, filePath);
        }
        catch (Exception)
        {
            return CreateSongFromPath(filePath);
        }
    }

    private static Playlist? ProcessPlaylistFile(string filePath)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            List<string> songPaths = [];
            string dir = Path.GetDirectoryName(filePath) ?? string.Empty;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                string songPath = line.Trim();

                if (!Path.IsPathRooted(songPath))
                {
                    songPath = Path.GetFullPath(Path.Combine(dir, songPath));
                }

                if (File.Exists(songPath))
                {
                    songPaths.Add(songPath);
                }
            }

            return songPaths.Count == 0
                ? null
                : new Playlist
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath,
                    Type = PlaylistType.File,
                    SongFilePaths = songPaths
                };
        }
        catch
        {
            return null;
        }
    }

    private static Song CreateSongFromTags(TagLib.File tagFile, string filePath)
    {
        bool hasArt = tagFile.Tag.Pictures != null && tagFile.Tag.Pictures.Length > 0;

        return new()
        {
            FilePath = filePath,
            Title = GetSongTitle(tagFile, filePath),
            Artist = tagFile.Tag.Performers.FirstOrDefault() ?? "Unknown Artist",
            Album = tagFile.Tag.Album ?? "Unknown Album",
            Duration = tagFile.Properties.Duration,
            HasArt = hasArt
        };
    }

    private static string GetSongTitle(TagLib.File tagFile, string filePath)
    {
        return string.IsNullOrWhiteSpace(tagFile.Tag.Title)
            ? Path.GetFileNameWithoutExtension(filePath)
            : tagFile.Tag.Title;
    }

    private static Song CreateSongFromPath(string filePath)
    {
        return new()
        {
            FilePath = filePath,
            Title = Path.GetFileNameWithoutExtension(filePath),
            Artist = "Unknown Artist",
            Album = "Unknown Album",
            Duration = TimeSpan.Zero,
            HasArt = false
        };
    }
}