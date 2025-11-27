namespace Sonorize.Core.Helpers;

public static class AppDataHelper
{
    private const string AppName = "Sonorize";

    /// <summary>
    /// Gets the root storage directory for the application.
    /// (e.g., %APPDATA%\Sonorize or ~/.config/Sonorize)
    /// </summary>
    public static string GetBaseDirectory()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppName);
    }

    /// <summary>
    /// Gets a specific subdirectory within the application storage, creating it if it doesn't exist.
    /// </summary>
    public static string GetSubDirectory(string subDirectoryName)
    {
        string path = Path.Combine(GetBaseDirectory(), subDirectoryName);

        if (!Directory.Exists(path))
        {
            _ = Directory.CreateDirectory(path);
        }

        return path;
    }
}