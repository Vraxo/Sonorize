using System.Text.Json.Serialization;

namespace Sonorize.Core.Settings;

public class LibrarySettings
{
    public bool ScanOnStartup { get; set; } = true;

    public List<string> MusicFolderPaths { get; set; } = [];

    public List<string> SupportedFileExtensions { get; set; } =
    [
        ".mp3", ".flac", ".m4a", ".aac", ".wav", ".ogg"
    ];

    // Individual View Modes
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LibraryViewMode SongsViewMode { get; set; } = LibraryViewMode.List;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LibraryViewMode AlbumsViewMode { get; set; } = LibraryViewMode.Grid;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LibraryViewMode ArtistsViewMode { get; set; } = LibraryViewMode.Grid;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LibraryViewMode PlaylistsViewMode { get; set; } = LibraryViewMode.Grid;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LibraryViewMode PlaylistDetailViewMode { get; set; } = LibraryViewMode.List; // NEW

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TextOverflowMode TrackListOverflow { get; set; } = TextOverflowMode.Wrap;

    // Grid Customization
    public int GridItemWidth { get; set; } = 180;
    public int GridGap { get; set; } = 24;
    public int GridItemPadding { get; set; } = 16;

    // List Customization
    public int ListArtSize { get; set; } = 40;

    public TrackListColumnSettings Columns { get; set; } = new();
}