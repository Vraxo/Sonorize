using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public interface IMusicLibraryService
{
    Task<List<Song>> LoadSongsFromFolderAsync(string folderPath, IEnumerable<string> extensions, CancellationToken cancellationToken);
    Task<List<Playlist>> LoadPlaylistsFromFolderAsync(string folderPath, CancellationToken cancellationToken);

    SongMetadata? GetMetadata(string filePath);
    bool SaveMetadata(string filePath, SongMetadata metadata);

    // NEW: Expose parsing logic for dropped files
    Task<Song?> CreateSongFromFileAsync(string filePath);
}