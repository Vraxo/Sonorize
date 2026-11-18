using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Sonorize.Core.Theming;

/// <summary>
/// Reads and writes versioned Sonorize theme packages.
/// </summary>
public static class ThemePackageArchive
{
    public const string FileExtension = ".sonorize-theme";

    private const int MaxAssetCount = 64;
    private const int MaxArchiveEntryCount = MaxAssetCount + 4;
    private const int MaxJsonEntryBytes = 1024 * 1024;
    private const int MaxStylesBytes = 2 * 1024 * 1024;
    private const int MaxAssetBytes = 8 * 1024 * 1024;
    private const long MaxArchiveBytes = 32L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private static readonly HashSet<string> AllowedAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avif",
        ".gif",
        ".ico",
        ".jpeg",
        ".jpg",
        ".otf",
        ".png",
        ".svg",
        ".ttf",
        ".webp",
        ".woff",
        ".woff2"
    };

    /// <summary>
    /// Writes a theme package to a ZIP stream without closing the destination stream.
    /// </summary>
    public static void Write(Stream destination, ThemePackage package)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(package);

        if (!destination.CanWrite)
        {
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        }

        ValidatePackage(package);

        using ZipArchive archive = new(destination, ZipArchiveMode.Create, leaveOpen: true);

        WriteJsonEntry(archive, "manifest.json", package.Manifest);
        WriteJsonEntry(archive, "colors.json", package.Colors);

        ThemeLayout layout = package.Layout.Clone();
        layout.CustomCss = "";
        WriteJsonEntry(archive, "layout.json", layout);
        WriteTextEntry(archive, "styles.css", package.Layout.CustomCss);

        foreach ((string assetPath, byte[] content) in package.Assets.OrderBy(
            pair => pair.Key,
            StringComparer.OrdinalIgnoreCase))
        {
            string safePath = ValidateAssetPath(assetPath);
            ZipArchiveEntry entry = archive.CreateEntry($"assets/{safePath}", CompressionLevel.Optimal);

            using Stream entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }
    }

    /// <summary>
    /// Reads a theme package from a seekable ZIP stream without closing the source stream.
    /// </summary>
    public static ThemePackage Read(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanRead || !source.CanSeek)
        {
            throw new ArgumentException("The source stream must be readable and seekable.", nameof(source));
        }

        using ZipArchive archive = new(source, ZipArchiveMode.Read, leaveOpen: true);

        if (archive.Entries.Count > MaxArchiveEntryCount)
        {
            throw new InvalidDataException("The theme package contains too many entries.");
        }

        Dictionary<string, ZipArchiveEntry> entries = new(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        int assetCount = 0;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string entryName = entry.FullName;

            if (string.IsNullOrWhiteSpace(entryName) || entryName.EndsWith('/'))
            {
                throw new InvalidDataException("Directory and empty ZIP entries are not supported.");
            }

            int unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixFileType == 0xA000)
            {
                throw new InvalidDataException("Symbolic links are not supported in theme packages.");
            }

            long entryLimit;
            if (entryName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = entryName["assets/".Length..];
                _ = ValidateAssetPath(assetPath);
                entryLimit = MaxAssetBytes;
                assetCount++;

                if (assetCount > MaxAssetCount)
                {
                    throw new InvalidDataException("The theme package contains too many assets.");
                }
            }
            else if (IsKnownRootEntry(entryName))
            {
                entryLimit = entryName.Equals("styles.css", StringComparison.OrdinalIgnoreCase)
                    ? MaxStylesBytes
                    : MaxJsonEntryBytes;
            }
            else
            {
                throw new InvalidDataException($"Unsupported theme package entry: {entryName}");
            }

            if (entry.Length < 0 || entry.Length > entryLimit)
            {
                throw new InvalidDataException($"Theme package entry exceeds its size limit: {entryName}");
            }

            totalBytes += entry.Length;
            if (totalBytes > MaxArchiveBytes)
            {
                throw new InvalidDataException("The theme package exceeds the total uncompressed size limit.");
            }

            if (!entries.TryAdd(entryName, entry))
            {
                throw new InvalidDataException($"Duplicate theme package entry: {entryName}");
            }
        }

        ThemePackageManifest manifest = ReadRequiredJson<ThemePackageManifest>(entries, "manifest.json");
        ThemeColors colors = ReadRequiredJson<ThemeColors>(entries, "colors.json");
        ThemeLayout layout = ReadRequiredJson<ThemeLayout>(entries, "layout.json");

        string stylesCss = entries.TryGetValue("styles.css", out ZipArchiveEntry? stylesEntry)
            ? ReadTextEntry(stylesEntry, MaxStylesBytes)
            : "";

        Dictionary<string, byte[]> assets = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string entryName, ZipArchiveEntry entry) in entries)
        {
            if (!entryName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string assetPath = ValidateAssetPath(entryName["assets/".Length..]);
            assets.Add(assetPath, ReadEntryBytes(entry, MaxAssetBytes));
        }

        layout.CustomCss = stylesCss;

        ThemePackage package = new()
        {
            Manifest = manifest,
            Colors = colors,
            Layout = layout,
            Assets = assets
        };

        ValidatePackage(package);
        return package;
    }

    private static void ValidatePackage(ThemePackage package)
    {
        if (package.Manifest is null || package.Colors is null || package.Layout is null || package.Assets is null)
        {
            throw new InvalidDataException("The theme package is missing required content.");
        }

        if (package.Manifest.FormatVersion != ThemePackageManifest.CurrentFormatVersion)
        {
            throw new InvalidDataException($"Unsupported theme package format version: {package.Manifest.FormatVersion}");
        }

        if (string.IsNullOrWhiteSpace(package.Manifest.Name) || package.Manifest.Name.Length > 128)
        {
            throw new InvalidDataException("The theme package name must contain between 1 and 128 characters.");
        }

        if (package.Manifest.Description.Length > 2048 ||
            package.Manifest.Author.Length > 128 ||
            package.Manifest.ThemeVersion.Length > 64)
        {
            throw new InvalidDataException("Theme package metadata exceeds its size limit.");
        }

        if (package.Layout.CustomCss is null ||
            StrictUtf8.GetByteCount(package.Layout.CustomCss) > MaxStylesBytes)
        {
            throw new InvalidDataException("The theme package stylesheet exceeds its size limit.");
        }

        if (package.Assets.Count > MaxAssetCount)
        {
            throw new InvalidDataException("The theme package contains too many assets.");
        }

        long totalAssetBytes = 0;
        foreach ((string path, byte[] content) in package.Assets)
        {
            _ = ValidateAssetPath(path);

            if (content is null || content.Length > MaxAssetBytes)
            {
                throw new InvalidDataException($"Theme package asset exceeds its size limit: {path}");
            }

            totalAssetBytes += content.Length;
            if (totalAssetBytes > MaxArchiveBytes)
            {
                throw new InvalidDataException("Theme package assets exceed the total size limit.");
            }
        }
    }

    private static bool IsKnownRootEntry(string entryName)
    {
        return entryName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
            || entryName.Equals("colors.json", StringComparison.OrdinalIgnoreCase)
            || entryName.Equals("layout.json", StringComparison.OrdinalIgnoreCase)
            || entryName.Equals("styles.css", StringComparison.OrdinalIgnoreCase);
    }

    private static string ValidateAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || path.StartsWith('/')
            || path.Contains('\\')
            || path.Contains(':')
            || path.Contains('\0'))
        {
            throw new InvalidDataException("Theme package asset paths must be relative.");
        }

        string[] segments = path.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException($"Invalid theme package asset path: {path}");
        }

        string extension = Path.GetExtension(segments[^1]);
        if (!AllowedAssetExtensions.Contains(extension))
        {
            throw new InvalidDataException($"Unsupported theme package asset type: {extension}");
        }

        return string.Join('/', segments);
    }

    private static void WriteJsonEntry<T>(ZipArchive archive, string entryName, T value)
    {
        byte[] content = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (content.Length > MaxJsonEntryBytes)
        {
            throw new InvalidDataException($"Theme package JSON exceeds its size limit: {entryName}");
        }

        WriteEntry(archive, entryName, content);
    }

    private static void WriteTextEntry(ZipArchive archive, string entryName, string value)
    {
        byte[] content = new UTF8Encoding(false).GetBytes(value);
        if (content.Length > MaxStylesBytes)
        {
            throw new InvalidDataException("The theme package stylesheet exceeds its size limit.");
        }

        WriteEntry(archive, entryName, content);
    }

    private static void WriteEntry(ZipArchive archive, string entryName, byte[] content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using Stream stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }

    private static T ReadRequiredJson<T>(
        IReadOnlyDictionary<string, ZipArchiveEntry> entries,
        string entryName)
    {
        if (!entries.TryGetValue(entryName, out ZipArchiveEntry? entry))
        {
            throw new InvalidDataException($"Required theme package entry is missing: {entryName}");
        }

        byte[] content = ReadEntryBytes(entry, MaxJsonEntryBytes);
        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions)
                ?? throw new InvalidDataException($"Theme package entry is empty: {entryName}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Theme package entry contains invalid JSON: {entryName}", ex);
        }
    }

    private static string ReadTextEntry(ZipArchiveEntry entry, int maxBytes)
    {
        byte[] content = ReadEntryBytes(entry, maxBytes);
        try
        {
            return StrictUtf8.GetString(content);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("The theme package stylesheet is not valid UTF-8.", ex);
        }
    }

    private static byte[] ReadEntryBytes(ZipArchiveEntry entry, int maxBytes)
    {
        using Stream source = entry.Open();
        using MemoryStream destination = new();

        byte[] buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (destination.Length + bytesRead > maxBytes)
            {
                throw new InvalidDataException($"Theme package entry exceeds its size limit: {entry.FullName}");
            }

            destination.Write(buffer, 0, bytesRead);
        }

        return destination.ToArray();
    }
}
