using Sonorize.Core.Models;

namespace Sonorize.Core.Services.Library;

public class SearchService
{
    public IReadOnlyList<Song> Search(IEnumerable<Song> source, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return source.ToList();
        }

        // Tokenize query: "Pink Time" -> ["Pink", "Time"]
        // All tokens must match at least one field (Title, Artist, or Album).
        string[] tokens = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length == 0
            ? source.ToList()
            : source
            .Where(s => tokens.All(token =>
                s.Title.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                s.Artist.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                s.Album.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}