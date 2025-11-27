namespace Sonorize.Core.Settings;

public class LastfmSettings
{
    public bool ScrobblingEnabled { get; set; } = false;
    public string? Username { get; set; }

    public string? SessionKey { get; set; }

    public int ScrobbleThresholdPercentage { get; set; } = 50;
    public int ScrobbleThresholdAbsoluteSeconds { get; set; } = 240; // 4 minutes
}