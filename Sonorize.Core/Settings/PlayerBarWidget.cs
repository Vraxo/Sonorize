using System.Text.Json.Serialization;

namespace Sonorize.Core.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayerBarWidget
{
    Info,
    Transport,
    Seek,
    StackedTransport,
    Volume,
    // Granular Tools
    Focus,
    SpeedPitch,
    Equalizer,
    Queue,
    Spacer
}