using System.Text.Json.Serialization;

namespace Sonorize.Core.Models;

public class Playlist
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Playlist";

    [JsonPropertyName("songFilePaths")]
    public List<string> SongFilePaths { get; set; } = [];

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlaylistType Type { get; set; } = PlaylistType.Manual;

    [JsonIgnore]
    public string? FilePath { get; set; }
}