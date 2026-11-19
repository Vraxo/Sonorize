using Sonorize.Core.Library;

namespace Sonorize.Core.Tests.Library;

public class LibrarySettingsTests
{
    [Fact]
    public void Constructor_IncludesOpusInDefaultSupportedExtensions()
    {
        LibrarySettings settings = new();

        Assert.Contains(".opus", settings.SupportedFileExtensions);
    }

    [Fact]
    public void ApplyOpusDefaultMigration_AddsOpusToLegacyDefaults()
    {
        LibrarySettings settings = new()
        {
            SupportedFileExtensions =
            [
                ".mp3",
                ".flac",
                ".m4a",
                ".aac",
                ".wav",
                ".ogg"
            ]
        };

        bool migrated = settings.ApplyOpusDefaultMigration();

        Assert.True(migrated);
        Assert.Contains(".opus", settings.SupportedFileExtensions);
        Assert.Equal(7, settings.SupportedFileExtensions.Count);
    }

    [Fact]
    public void ApplyOpusDefaultMigration_PreservesCustomExtensionSelections()
    {
        LibrarySettings settings = new()
        {
            SupportedFileExtensions =
            [
                ".mp3",
                ".flac",
                ".custom"
            ]
        };

        bool migrated = settings.ApplyOpusDefaultMigration();

        Assert.False(migrated);
        Assert.Equal([".mp3", ".flac", ".custom"], settings.SupportedFileExtensions);
    }
}
