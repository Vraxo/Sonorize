using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class ScanResult
{
    public List<Song> Songs { get; set; } = [];
    public List<Playlist> Playlists { get; set; } = [];
}