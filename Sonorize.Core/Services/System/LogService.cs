using Sonorize.Core.Helpers;

namespace Sonorize.Core.Services.System;

public class LogService
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService()
    {
        string dir = AppDataHelper.GetBaseDirectory();

        if (!Directory.Exists(dir))
        {
            _ = Directory.CreateDirectory(dir);
        }

        _logPath = Path.Combine(dir, "app.log");
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warn(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        string msg = ex is null
            ? message
            : $"{message}\n{ex}";

        Write("ERROR", msg);
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                // Simple rolling log: if > 5MB, delete and start over
                NameMe();

                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";

                File.AppendAllText(_logPath, line);
            }
        }
        catch
        {
            // Fallback to console if file access fails (better than crashing the crash logger)
            Console.WriteLine($"[LOG FAILURE] {message}");
        }
    }

    private void NameMe()
    {
        const int Mb5 = 5 * 1024 * 1024;

        if (!File.Exists(_logPath) || new FileInfo(_logPath).Length <= Mb5)
        {
            return;
        }

        File.Delete(_logPath);
    }
}