using System.Text.Json.Serialization;

namespace Sonorize.Core.Playback;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayerBarPosition
{
    Bottom,
    Top
}
