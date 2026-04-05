namespace Sonorize.Core.Helpers;

public static class AppDataHelper
{
    private const string AppName = "Sonorize";

    public static string GetBaseDirectory()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppName);
    }

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