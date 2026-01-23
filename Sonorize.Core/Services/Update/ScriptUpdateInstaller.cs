using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sonorize.Core.Services.Update;

public static class ScriptUpdateInstaller
{
    public static void InstallAndRestart(string zipPath)
    {
        string appDir = AppContext.BaseDirectory;
        string appExe = Process.GetCurrentProcess().MainModule?.FileName ?? "Sonorize.exe";
        int currentPid = Environment.ProcessId;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RunWindowsInstaller(currentPid, zipPath, appDir, appExe);
        }
        else
        {
            RunUnixInstaller(currentPid, zipPath, appDir, appExe);
        }

        // Kill current process to allow overwrite
        Environment.Exit(0);
    }

    private static void RunWindowsInstaller(int pid, string zipPath, string appDir, string appExe)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), "sonorize_update.ps1");

        // PowerShell Script: Wait for exit -> Expand ZIP -> Delete ZIP -> Restart
        string script = $@"
$ErrorActionPreference = 'Stop'
Write-Host 'Waiting for Sonorize to close...'
Wait-Process -Id {pid} -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host 'Updating files...'
Expand-Archive -LiteralPath '{zipPath}' -DestinationPath '{appDir}' -Force

Write-Host 'Cleaning up...'
Remove-Item '{zipPath}' -Force

Write-Host 'Restarting...'
Start-Process '{appExe}'
";
        File.WriteAllText(scriptPath, script);

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _ = Process.Start(startInfo);
    }

    private static void RunUnixInstaller(int pid, string zipPath, string appDir, string appExe)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), "sonorize_update.sh");

        // Bash Script
        string script = $@"
#!/bin/bash
while kill -0 {pid} 2>/dev/null; do sleep 1; done
unzip -o ""{zipPath}"" -d ""{appDir}""
rm ""{zipPath}""
""{appExe}"" &
";
        File.WriteAllText(scriptPath, script);

        // Make executable
        Process.Start("chmod", $"+x \"{scriptPath}\"").WaitForExit();

        ProcessStartInfo startInfo = new()
        {
            FileName = "/bin/bash",
            Arguments = scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _ = Process.Start(startInfo);
    }
}