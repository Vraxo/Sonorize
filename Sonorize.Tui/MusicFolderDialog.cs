using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>Prompts for a music folder and reports validation errors.</summary>
internal sealed class MusicFolderDialog
{
    private readonly Runnable _window;
    private readonly MusicFolderService _folders;

    public MusicFolderDialog(Runnable window, MusicFolderService folders)
    {
        _window = window;
        _folders = folders;
    }

    public void Show()
    {
        TextField pathField = new() { Width = Dim.Fill(0, 50), Height = 1 };
        string? entered = _window.Prompt<TextField, string>(pathField,
            input: Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            beginInitHandler: prompt => prompt.Title = "Add music folder");

        if (string.IsNullOrWhiteSpace(entered))
            return;

        AddFolderResult result = _folders.Add(entered, out string path);
        if (result == AddFolderResult.NotFound)
            MessageBox.ErrorQuery(_window.App!, "Add music folder", $"Folder not found:\n{path}", "OK");
        else if (result == AddFolderResult.AlreadyAdded)
            MessageBox.Query(_window.App!, "Add music folder", "That folder is already in your library.", "OK");
    }
}
