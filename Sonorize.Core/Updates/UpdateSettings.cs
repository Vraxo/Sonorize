namespace Sonorize.Core.Updates;

public class UpdateSettings
{
    public bool CheckForUpdates { get; set; } = true;
    public bool IncludePreReleases { get; set; } = false;
    public DateTime? LastCheckTime { get; set; }
}
