namespace Sonorize.Tui;

/// <summary>Formats missing values consistently in browse labels and table cells.</summary>
internal static class BrowseText
{
    public static string Cell(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
