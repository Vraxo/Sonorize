namespace Sonorize.Core.Core;

public class LogService
{
    private const long MaxLogSizeBytes = 5 * 1024 * 1024;

    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService()
    {
        string directory = AppDataHelper.GetBaseDirectory();
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, "app.log");
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
        string details = ex is null ? message : $"{message}{Environment.NewLine}{ex}";
        Write("ERROR", details);
    }

    private void Write(string level, string message)
    {
        string line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] {message}";

        lock (_lock)
        {
            WriteToConsole(line);
            WriteToFile(line);
        }
    }

    private static void WriteToConsole(string line)
    {
        try
        {
            Console.WriteLine(line);
        }
        catch
        {
            // Logging must never interrupt the application.
        }
    }

    private void WriteToFile(string line)
    {
        try
        {
            RotateLogIfNeeded();
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            WriteToConsole($"[LOG FAILURE] Could not write '{_logPath}': {ex.Message}");
        }
    }

    private void RotateLogIfNeeded()
    {
        if (!File.Exists(_logPath) || new FileInfo(_logPath).Length <= MaxLogSizeBytes)
        {
            return;
        }

        string previousLogPath = $"{_logPath}.old";

        if (File.Exists(previousLogPath))
        {
            File.Delete(previousLogPath);
        }

        File.Move(_logPath, previousLogPath);
    }
}
