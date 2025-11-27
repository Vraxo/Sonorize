namespace Sonorize.Core.Models;

public class Song
{
    public required string FilePath { get; set; }
    public string Title { get; set; } = "Unknown Title";
    public string Artist { get; set; } = "Unknown Artist";
    public string Album { get; set; } = "Unknown Album";
    public TimeSpan Duration { get; set; }

    // Replaced heavy byte array with a lightweight flag
    public bool HasArt { get; set; }

    public string DurationString => $"{(int)Duration.TotalMinutes}:{Duration.Seconds:d2}";
}