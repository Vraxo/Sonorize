using Sonorize.Core.Models;
using System.IO.Compression;

namespace Sonorize.Core.Helpers;

public static class MockUpdateGenerator
{
    public static ReleaseInfo Generate()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "SonorizeMock_" + Guid.NewGuid());
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            // Create a dummy file that proves the update worked
            File.WriteAllText(Path.Combine(tempDir, "UPDATE_SUCCESSFUL.txt"), $"Update test completed at {DateTime.Now}");

            string zipPath = Path.Combine(Path.GetTempPath(), "mock-release.zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(tempDir, zipPath);

            return new()
            {
                Version = "v99.9.9-TEST",
                ReleaseNotes =
                    "# Test Update\nThis is a simulated update to test the installation pipeline.\n\n" +
                    "- Verifies ZIP extraction\n" +
                    "- Verifies Script execution\n" +
                    "- Verifies App Restart",
                DownloadUrl = zipPath,
                SizeBytes = new FileInfo(zipPath).Length,
                IsMandatory = false
            };
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}