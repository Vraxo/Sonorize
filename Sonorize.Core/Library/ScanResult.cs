using Sonorize.Core.Core;
using Sonorize.Core.Playlists;

namespace Sonorize.Core.Library;

public class ScanResult
{
    public List<Song> Songs { get; set; } = [];
    public List<Playlist> Playlists { get; set; } = [];
}
