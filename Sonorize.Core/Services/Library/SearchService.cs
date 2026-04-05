using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class SearchService
{
    public static IReadOnlyList<Song> Search(IEnumerable<Song> source, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [.. source];
        }

        string[] tokens = query
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length == 0
            ? [.. source]
            : GetSongs(source, tokens);
    }

    private static List<Song> GetSongs(IEnumerable<Song> source, string[] tokens)
    {
        return [.. source
            .Where(s => tokens.All(token =>
                s.Title.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                s.Artist.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                s.Album.Contains(token, StringComparison.OrdinalIgnoreCase)))];
    }
}