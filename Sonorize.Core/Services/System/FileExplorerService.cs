using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sonorize.Core.Services.System;

public class FileExplorerService
{
    public void ShowInFolder(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        // Clean path for safety
        string path = Path.GetFullPath(filePath);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows Explorer needs specific quoting for the /select argument
                _ = Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS 'open -R' reveals the file in Finder
                var info = new ProcessStartInfo("open")
                {
                    UseShellExecute = true
                };
                info.ArgumentList.Add("-R");
                info.ArgumentList.Add(path);
                _ = Process.Start(info);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux (xdg-open) generally opens the parent directory
                string? folder = Path.GetDirectoryName(path);
                if (folder != null)
                {
                    var info = new ProcessStartInfo("xdg-open")
                    {
                        UseShellExecute = true
                    };
                    info.ArgumentList.Add(folder);
                    _ = Process.Start(info);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to open file explorer: {ex.Message}");
        }
    }
}