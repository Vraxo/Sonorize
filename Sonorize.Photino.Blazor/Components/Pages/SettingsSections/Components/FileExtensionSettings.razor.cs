using Microsoft.AspNetCore.Components.Web;
using Sonorize.Core.Services.Library;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class FileExtensionSettings
{
    private readonly List<string> _knownExtensions =
    [
        ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".wma", ".opus", ".aiff"
    ];

    private string _newExtension = "";

    private bool IsActive(string ext)
    {
        return AppSettings.Library.SupportedFileExtensions.Contains(ext);
    }

    private void ToggleExtension(string ext)
    {
        if (IsActive(ext))
        {
            _ = AppSettings.Library.SupportedFileExtensions.Remove(ext);
        }
        else
        {
            AppSettings.Library.SupportedFileExtensions.Add(ext);
        }

        SaveAndRefresh();
    }

    private IEnumerable<string> GetCustomExtensions()
    {
        return AppSettings.Library.SupportedFileExtensions
            .Where(e => !_knownExtensions.Contains(e))
            .OrderBy(e => e);
    }

    private void HandleExtensionKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddExtension();
        }
    }

    private void AddExtension()
    {
        if (string.IsNullOrWhiteSpace(_newExtension))
        {
            return;
        }

        string formatted = _newExtension.Trim().ToLowerInvariant();
        if (!formatted.StartsWith("."))
        {
            formatted = "." + formatted;
        }

        if (!AppSettings.Library.SupportedFileExtensions.Contains(formatted))
        {
            AppSettings.Library.SupportedFileExtensions.Add(formatted);
            SaveAndRefresh();
        }
        _newExtension = "";
    }

    private void RemoveExtension(string ext)
    {
        _ = AppSettings.Library.SupportedFileExtensions.Remove(ext);
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
        SettingsManager.Save(AppSettings);
        // Trigger a refresh so the new file types are picked up immediately
        _ = LibraryService.RefreshLibraryAsync();
    }
}