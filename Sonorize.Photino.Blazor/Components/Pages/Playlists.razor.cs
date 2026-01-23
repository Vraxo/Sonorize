using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Playlists
{
    private List<Playlist> _playlists = [];
    private string _sortColumn = "Name";
    private bool _isAscending = true;

    protected override void OnInitialized()
    {
        LibService.LibraryChanged += OnLibraryChanged;
        LoadData();
    }

    private void OnLibraryChanged()
    {
        _ = InvokeAsync(() => { LoadData(); StateHasChanged(); });
    }

    private void LoadData()
    {
        IEnumerable<Playlist> source = LibService.AllPlaylists ?? [];
        _playlists = ApplySort(source);
    }

    private static bool FilterPlaylist(Playlist playlist, string query)
    {
        return playlist.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void Sort(string column)
    {
        if (_sortColumn == column)
        {
            _isAscending = !_isAscending;
        }
        else
        {
            _sortColumn = column;
            _isAscending = true;
        }

        LoadData();
    }

    private List<Playlist> ApplySort(IEnumerable<Playlist> source)
    {
        return _sortColumn switch
        {
            "Name" => _isAscending ? source.OrderBy(p => p.Name).ToList() : source.OrderByDescending(p => p.Name).ToList(),
            "Type" => _isAscending ? source.OrderBy(p => p.Type).ThenBy(p => p.Name).ToList() : source.OrderByDescending(p => p.Type).ThenBy(p => p.Name).ToList(),
            "Count" => _isAscending ? source.OrderBy(p => p.SongFilePaths.Count).ToList() : source.OrderByDescending(p => p.SongFilePaths.Count).ToList(),
            _ => [.. source]
        };
    }

    private RenderFragment RenderSortIcon(string column)
    {
        return _sortColumn != column
            ? (builder => { })
            : (builder =>
        {
            builder.OpenElement(0, "i");
            builder.AddAttribute(1, "class", _isAscending ? "fas fa-caret-up sort-icon" : "fas fa-caret-down sort-icon");
            builder.CloseElement();
        });
    }

    private void OnViewModeChanged(LibraryViewMode mode)
    {
        AppSettings.Library.PlaylistsViewMode = mode;
        SettingsManager.Save(AppSettings);
        StateHasChanged();
    }

    private void OpenPlaylist(Playlist playlist)
    {
        Nav.NavigateTo($"/playlist/{playlist.Id}");
    }

    private string? GetPlaylistArt(Playlist playlist)
    {
        foreach (string? path in playlist.SongFilePaths.Take(5))
        {
            Song? song = LibService.GetSong(path);

            if (song is null || !song.HasArt)
            {
                continue;
            }

            return song.FilePath;
        }

        return null;
    }

    private static string GetArtUrl(string path)
    {
        return $"sonorize://albumart/?path={Uri.EscapeDataString(path)}";
    }

    public void Dispose()
    {
        LibService.LibraryChanged -= OnLibraryChanged;
    }
}