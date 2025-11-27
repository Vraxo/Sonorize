using System.Text.Json.Serialization;

namespace Sonorize.Core.Settings;

public class PlaybackSettings
{
    public bool IsShuffle { get; set; } = false;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

    public bool PlayOnSingleClick { get; set; } = false;
    public float Volume { get; set; } = 0.75f;

    public string? OutputDeviceName { get; set; }

    // Equalizer State
    public bool EqEnabled { get; set; } = false;
    public float[] EqGains { get; set; } = new float[10];

    // FX State
    // Tempo is percentage change: 0 = 1.0x (normal), -50 = 0.5x, +100 = 2.0x
    public float Tempo { get; set; } = 0;

    // Pitch is in semitones: -12 to +12
    public float Pitch { get; set; } = 0;
}