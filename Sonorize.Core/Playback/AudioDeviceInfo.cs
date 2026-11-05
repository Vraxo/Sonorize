namespace Sonorize.Core.Playback;

public class AudioDeviceInfo
{
    public int Index { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }
}
