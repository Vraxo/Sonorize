using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sonorize.Core.Models;
using Sonorize.Core.Services.Library;
using System.Text;

namespace Sonorize.Photino.Blazor.Setup;

public static class SchemeHandlers
{
    // Security: Only allow specific extensions to be read via the sonorize:// scheme
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".ico", ".svg",
        // Audio (if needed for HTML5 players, though BASS handles main playback)
        ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".wma", ".opus"
    };

    public static Stream ProcessRequest(string url, LibraryService library, out string contentType)
    {
        contentType = "text/plain";

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return new MemoryStream(Encoding.UTF8.GetBytes("Invalid URI"));
        }

        if (uri.Host.Equals("albumart", StringComparison.OrdinalIgnoreCase))
        {
            return HandleAlbumArt(uri, library, out contentType);
        }
        else if (uri.Host.Equals("localfile", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLocalFile(uri, out contentType);
        }

        return new MemoryStream(Encoding.UTF8.GetBytes("Not Found"));
    }

    private static Stream HandleAlbumArt(Uri uri, LibraryService library, out string contentType)
    {
        contentType = "text/plain";

        if (!TryGetFilePath(uri, out string filePath))
        {
            return new MemoryStream([]);
        }

        // Handle Demo Data Art Generation
        if (filePath.StartsWith("demo://"))
        {
            contentType = "image/png";
            return GenerateDemoArt(filePath);
        }

        // Standard Local File Art
        if (!IsArtAvailable(library, filePath))
        {
            return new MemoryStream([]);
        }

        try
        {
            using TagLib.File tfile = TagLib.File.Create(filePath);
            TagLib.IPicture? pic = tfile.Tag.Pictures.FirstOrDefault();

            if (pic != null)
            {
                contentType = "image/jpeg";
                return new MemoryStream(pic.Data.Data);
            }
        }
        catch
        {
            // Return empty on error
        }

        return new MemoryStream([]);
    }

    private static Stream GenerateDemoArt(string filePath)
    {
        // Deterministic random based on string hash
        int seed = filePath.GetHashCode();
        var rng = new Random(seed);

        // Generate random nice colors
        var c1 = Color.FromRgb((byte)rng.Next(50, 255), (byte)rng.Next(50, 255), (byte)rng.Next(50, 255));
        var c2 = Color.FromRgb((byte)rng.Next(50, 255), (byte)rng.Next(50, 255), (byte)rng.Next(50, 255));

        // Create 200x200 image
        using var image = new Image<Rgba32>(200, 200);

        image.Mutate(x => x.Fill(new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(200, 200),
            GradientRepetitionMode.None,
            new ColorStop(0, c1),
            new ColorStop(1, c2)
        )));

        var ms = new MemoryStream();
        image.SaveAsPng(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream HandleLocalFile(Uri uri, out string contentType)
    {
        contentType = "application/octet-stream";

        if (!TryGetFilePath(uri, out string filePath) || !File.Exists(filePath))
        {
            return new MemoryStream([]);
        }

        // Security Check: Prevent arbitrary file reads (e.g. sensitive config files)
        string ext = Path.GetExtension(filePath);
        if (!AllowedExtensions.Contains(ext))
        {
            return new MemoryStream(Encoding.UTF8.GetBytes("Access Denied: File type not allowed via scheme."));
        }

        try
        {
            contentType = GetMimeType(filePath);
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch
        {
            return new MemoryStream([]);
        }
    }

    private static bool TryGetFilePath(Uri uri, out string filePath)
    {
        string query = uri.Query.TrimStart('?');
        const string prefix = "path=";

        if (query.StartsWith(prefix))
        {
            string encoded = query[prefix.Length..];
            filePath = Uri.UnescapeDataString(encoded);
            return true;
        }

        filePath = string.Empty;
        return false;
    }

    private static bool IsArtAvailable(LibraryService library, string filePath)
    {
        Song? song = library.GetSong(filePath);
        return song is { HasArt: true } && File.Exists(filePath);
    }

    private static string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            // Images
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            // Audio
            ".mp3" => "audio/mpeg",
            ".flac" => "audio/flac",
            ".wav" => "audio/wav",
            ".m4a" or ".aac" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".wma" => "audio/x-ms-wma",
            ".opus" => "audio/opus",
            _ => "application/octet-stream"
        };
    }
}