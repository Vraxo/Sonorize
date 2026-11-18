using System.IO.Compression;
using System.Text.Json;
using Sonorize.Core.Theming;

namespace Sonorize.Core.Tests.Theming;

public class ThemePackageArchiveTests
{
    [Fact]
    public void WriteAndRead_RoundTripsThemeContentAndAssets()
    {
        using MemoryStream stream = new();
        ThemePackage package = new()
        {
            Manifest = new ThemePackageManifest
            {
                Name = "Midnight",
                Description = "A restrained dark theme",
                Author = "Sonorize",
                ThemeVersion = "1.2.0"
            },
            Colors = new ThemeColors
            {
                AccentColor = "#AABBCC",
                BgPrimary = "#101010",
                TextPrimary = "#F0F0F0"
            },
            Layout = new ThemeLayout
            {
                BorderRadius = 10,
                BaseFontSize = 16,
                BackgroundImagePath = "assets/backgrounds/cover.webp",
                CustomCss = ".player-bar { opacity: 0.9; }"
            },
            Assets =
            {
                ["backgrounds/cover.webp"] = [1, 2, 3, 4],
                ["fonts/example.woff2"] = [5, 6, 7]
            }
        };

        ThemePackageArchive.Write(stream, package);

        Assert.True(stream.CanRead);
        stream.Position = 0;

        using (ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            string[] entries = archive.Entries
                .Select(entry => entry.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "assets/backgrounds/cover.webp",
                    "assets/fonts/example.woff2",
                    "colors.json",
                    "layout.json",
                    "manifest.json",
                    "styles.css"
                ],
                entries);

            ZipArchiveEntry layoutEntry = Assert.IsType<ZipArchiveEntry>(archive.GetEntry("layout.json"));
            using Stream layoutStream = layoutEntry.Open();
            using JsonDocument layoutJson = JsonDocument.Parse(layoutStream);
            Assert.Equal("", layoutJson.RootElement.GetProperty("CustomCss").GetString());
        }

        stream.Position = 0;
        ThemePackage loaded = ThemePackageArchive.Read(stream);

        Assert.Equal(ThemePackageManifest.CurrentFormatVersion, loaded.Manifest.FormatVersion);
        Assert.Equal("Midnight", loaded.Manifest.Name);
        Assert.Equal("A restrained dark theme", loaded.Manifest.Description);
        Assert.Equal("Sonorize", loaded.Manifest.Author);
        Assert.Equal("1.2.0", loaded.Manifest.ThemeVersion);

        Assert.Equal("#AABBCC", loaded.Colors.AccentColor);
        Assert.Equal("#101010", loaded.Colors.BgPrimary);
        Assert.Equal("#F0F0F0", loaded.Colors.TextPrimary);

        Assert.Equal(10, loaded.Layout.BorderRadius);
        Assert.Equal(16, loaded.Layout.BaseFontSize);
        Assert.Equal("assets/backgrounds/cover.webp", loaded.Layout.BackgroundImagePath);
        Assert.Equal(".player-bar { opacity: 0.9; }", loaded.Layout.CustomCss);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, loaded.Assets["backgrounds/cover.webp"]);
        Assert.Equal(new byte[] { 5, 6, 7 }, loaded.Assets["fonts/example.woff2"]);
    }

    [Fact]
    public void Write_RejectsNonHexColorValues()
    {
        using MemoryStream stream = new();
        ThemePackage package = new()
        {
            Manifest = new ThemePackageManifest { Name = "Unsafe" },
            Colors = new ThemeColors
            {
                AccentColor = "#fff; background-image: url(https://example.invalid)"
            }
        };

        Assert.Throws<InvalidDataException>(() => ThemePackageArchive.Write(stream, package));
    }

    [Fact]
    public void Write_RejectsAssetPathTraversal()
    {
        using MemoryStream stream = new();
        ThemePackage package = new()
        {
            Manifest = new ThemePackageManifest { Name = "Unsafe" },
            Assets =
            {
                ["../outside.png"] = [1]
            }
        };

        Assert.Throws<InvalidDataException>(() => ThemePackageArchive.Write(stream, package));
    }

    [Fact]
    public void Read_RejectsAssetPathTraversal()
    {
        using MemoryStream stream = CreateArchiveWithEntry(
            "assets/../outside.png",
            new ThemePackageManifest { Name = "Unsafe" });

        Assert.Throws<InvalidDataException>(() => ThemePackageArchive.Read(stream));
    }

    [Fact]
    public void Read_RejectsUnsupportedFormatVersion()
    {
        using MemoryStream stream = CreateArchiveWithEntry(
            null,
            new ThemePackageManifest
            {
                FormatVersion = ThemePackageManifest.CurrentFormatVersion + 1,
                Name = "Future"
            });

        Assert.Throws<InvalidDataException>(() => ThemePackageArchive.Read(stream));
    }

    [Fact]
    public void Read_RejectsUnsupportedAssetTypes()
    {
        using MemoryStream stream = CreateArchiveWithEntry(
            "assets/script.js",
            new ThemePackageManifest { Name = "Unsafe" });

        Assert.Throws<InvalidDataException>(() => ThemePackageArchive.Read(stream));
    }

    private static MemoryStream CreateArchiveWithEntry(
        string? assetEntry,
        ThemePackageManifest manifest)
    {
        MemoryStream stream = new();

        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "manifest.json", JsonSerializer.Serialize(manifest));
            WriteEntry(archive, "colors.json", JsonSerializer.Serialize(new ThemeColors()));
            WriteEntry(archive, "layout.json", JsonSerializer.Serialize(new ThemeLayout()));

            if (assetEntry is not null)
            {
                WriteEntry(archive, assetEntry, "untrusted content");
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using Stream stream = entry.Open();
        using StreamWriter writer = new(stream);
        writer.Write(content);
    }
}
