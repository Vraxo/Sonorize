namespace Sonorize.Core.Theming;

public sealed class ThemePackageManifest
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string ThemeVersion { get; set; } = "1.0.0";
}
