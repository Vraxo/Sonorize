using Sonorize.Core.Settings;

namespace Sonorize.Tui;

internal enum AddFolderResult
{
    Added,
    NotFound,
    AlreadyAdded
}

/// <summary>Validates and persists a music folder, then schedules its scan.</summary>
internal sealed class MusicFolderService
{
    private readonly SonorizeSettings _settings;
    private readonly ISettingsManager<SonorizeSettings> _settingsManager;
    private readonly Action<string> _scanFolder;

    public MusicFolderService(SonorizeSettings settings, ISettingsManager<SonorizeSettings> settingsManager,
        Action<string> scanFolder)
    {
        _settings = settings;
        _settingsManager = settingsManager;
        _scanFolder = scanFolder;
    }

    public AddFolderResult Add(string entered, out string path)
    {
        path = ExpandPath(entered.Trim());
        if (!Directory.Exists(path))
            return AddFolderResult.NotFound;
        if (_settings.Library.MusicFolderPaths.Contains(path))
            return AddFolderResult.AlreadyAdded;

        _settings.Library.MusicFolderPaths.Add(path);
        _settingsManager.Save(_settings);
        _scanFolder(path);
        return AddFolderResult.Added;
    }

    private static string ExpandPath(string path)
    {
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[1..].TrimStart('/', '\\'));
        }
        return Path.GetFullPath(path);
    }
}
