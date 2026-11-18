namespace Sonorize.Core.Theming;

public sealed class ThemePackage
{
    public ThemePackageManifest Manifest { get; set; } = new();
    public ThemeColors Colors { get; set; } = new();
    public ThemeLayout Layout { get; set; } = new();
    public string StylesCss { get; set; } = "";
    public Dictionary<string, byte[]> Assets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
