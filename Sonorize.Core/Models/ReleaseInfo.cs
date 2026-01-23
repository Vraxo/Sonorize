namespace Sonorize.Core.Models;

public class ReleaseInfo
{
    public required string Version { get; set; } // e.g. "v1.2.0"
    public required string DownloadUrl { get; set; }
    public required string ReleaseNotes { get; set; }
    public long SizeBytes { get; set; }
    public bool IsMandatory { get; set; }
}