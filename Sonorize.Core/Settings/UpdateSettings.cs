namespace Sonorize.Core.Settings;

public class UpdateSettings
{
    public bool CheckForUpdates { get; set; } = true;
    public bool IncludePreReleases { get; set; } = false;
    public DateTime? LastCheckTime { get; set; }
}