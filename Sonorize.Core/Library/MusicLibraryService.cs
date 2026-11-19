using Microsoft.Extensions.Logging;
using Sonorize.Core.Core;
using System.Collections.Concurrent;
using Sonorize.Core.Metadata;
using Sonorize.Core.Playlists;

namespace Sonorize.Core.Library;

public class MusicLibraryService : IMusicLibraryService
{
    private static readonly HashSet<string> PlaylistExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".m3u",
        ".m3u8"
    };

    private readonly ILogger<MusicLibraryService> _logger;

    public MusicLibraryService(ILogger<MusicLibraryService> logger)
    {
        _logger = logger;
    }

    public Task<List<Song>> LoadSongsFromFolderAsync(string folderPath, IEnumerable<string> extensions, CancellationToken cancellationToken)
    {
        HashSet<string> extensionSet = new(extensions, StringComparer.OrdinalIgnoreCase);

        return Task.Run(() =>
        {
            int directoryCount = 0;
            int totalFileCount = 0;
            Dictionary<string, int> extensionCounts = new(StringComparer.OrdinalIgnoreCase);

            List<string> musicFiles = FileSystemHelper.GetFilesSafe(
                folderPath,
                extensionSet,
                _ => directoryCount++,
                file =>
                {
                    totalFileCount++;
                    string extension = Path.GetExtension(file);
                    extension = string.IsNullOrEmpty(extension) ? "<no extension>" : extension;
                    extensionCounts[extension] = extensionCounts.GetValueOrDefault(extension) + 1;
                },
                (directory, exception) =>
                    _logger.LogWarning(exception, "Could not enumerate library directory {DirectoryPath}.", directory))
                .ToList();

            int unsupportedFileCount = totalFileCount - musicFiles.Count;
            string extensionSummary = string.Join(
                ", ",
                extensionCounts
                    .OrderByDescending(item => item.Value)
                    .Take(15)
                    .Select(item => $"{item.Key}: {item.Value}"));

            _logger.LogInformation(
                "Library enumeration for {FolderPath}: visited {DirectoryCount} director(ies), found {TotalFileCount} file(s), {SupportedFileCount} supported and {UnsupportedFileCount} unsupported. Extensions: {ExtensionSummary}",
                folderPath,
                directoryCount,
                totalFileCount,
                musicFiles.Count,
                unsupportedFileCount,
                string.IsNullOrEmpty(extensionSummary) ? "<none>" : extensionSummary);

            ConcurrentBag<Song> songs = [];
            int skippedFiles = 0;
            int metadataFallbacks = 0;

            _logger.LogInformation("Found {FileCount} supported audio file(s) in {FolderPath}.", musicFiles.Count, folderPath);

            try
            {
                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                };

                _ = Parallel.ForEach(musicFiles, parallelOptions, file =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!IsFileValid(file))
                    {
                        Interlocked.Increment(ref skippedFiles);
                        return;
                    }

                    Song song = ProcessMusicFile(file, out bool usedMetadataFallback);
                    songs.Add(song);

                    if (usedMetadataFallback)
                    {
                        Interlocked.Increment(ref metadataFallbacks);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Audio file scan cancelled for {FolderPath}.", folderPath);
                return [];
            }

            List<Song> loadedSongs = songs.ToList();
            string summary =
                "Audio scan results for {FolderPath}: {LoadedCount} loaded, {SkippedCount} skipped as invalid, {FallbackCount} loaded using filename fallback.";

            if (metadataFallbacks > 0 || skippedFiles > 0)
            {
                _logger.LogWarning(summary, folderPath, loadedSongs.Count, skippedFiles, metadataFallbacks);
            }
            else
            {
                _logger.LogInformation(summary, folderPath, loadedSongs.Count, skippedFiles, metadataFallbacks);
            }

            return loadedSongs;
        }, cancellationToken);
    }

    public Task<List<Playlist>> LoadPlaylistsFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            IEnumerable<string> playlistFiles = FileSystemHelper.GetFilesSafe(folderPath, PlaylistExtensions);
            List<Playlist> playlists = [];
            int skippedFiles = 0;

            foreach (string file in playlistFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!IsFileValid(file))
                {
                    skippedFiles++;
                    continue;
                }

                Playlist? playlist = ProcessPlaylistFile(file);

                if (playlist is null)
                {
                    skippedFiles++;
                    continue;
                }

                playlists.Add(playlist);
            }

            if (skippedFiles > 0)
            {
                _logger.LogWarning("Skipped {SkippedCount} invalid or unreadable playlist file(s) in {FolderPath}.", skippedFiles, folderPath);
            }

            return playlists;
        }, cancellationToken);
    }

    public Task<Song?> CreateSongFromFileAsync(string filePath)
    {
        return Task.Run(() =>
        {
            return !IsFileValid(filePath)
                ? null
                : ProcessMusicFile(filePath, out _);
        });
    }

    public SongMetadata? GetMetadata(string filePath)
    {
        if (!IsFileValid(filePath))
        {
            return null;
        }

        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);
            TagLib.Tag tag = file.Tag;

            return new()
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
            _logger.LogError(ex, "Failed to read metadata for {FilePath}.", filePath);
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
            using TagLib.File file = TagLib.File.Create(filePath);
            TagLib.Tag tag = file.Tag;

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
            _logger.LogError(ex, "Failed to save metadata for {FilePath}.", filePath);
            return false;
        }
    }

    private static bool IsFileValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            return new FileInfo(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private Song ProcessMusicFile(string filePath, out bool usedMetadataFallback)
    {
        try
        {
            using TagLib.File tagFile = TagLib.File.Create(filePath);
            usedMetadataFallback = false;
            return CreateSongFromTags(tagFile, filePath);
        }
        catch (Exception)
        {
            usedMetadataFallback = true;
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
                : new()
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
        bool hasArt = tagFile.Tag.Pictures is not null && tagFile.Tag.Pictures.Length > 0;

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
