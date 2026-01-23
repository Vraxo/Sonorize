using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Albums
{
    private List<AlbumGroup> _albums = [];
    private string _sortColumn = "Title";
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
        IReadOnlyList<AlbumGroup> source = LibService.AllAlbums;
        _albums = ApplySort(source);
    }

    private static bool FilterAlbum(AlbumGroup album, string query)
    {
        return album.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               album.Artist.Contains(query, StringComparison.OrdinalIgnoreCase);
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

    private List<AlbumGroup> ApplySort(IEnumerable<AlbumGroup> source)
    {
        return _sortColumn switch
        {
            "Title" => _isAscending ? [.. source.OrderBy(a => a.Title)] : source.OrderByDescending(a => a.Title).ToList(),
            "Artist" => _isAscending ? source.OrderBy(a => a.Artist).ThenBy(a => a.Title).ToList() : [.. source.OrderByDescending(a => a.Artist).ThenBy(a => a.Title)],
            "Count" => _isAscending ? [.. source.OrderBy(a => a.SongCount)] : source.OrderByDescending(a => a.SongCount).ToList(),
            _ => source.ToList()
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
        AppSettings.Library.AlbumsViewMode = mode;
        SettingsManager.Save(AppSettings);
        StateHasChanged();
    }

    private void OpenAlbum(AlbumGroup album)
    {
        Nav.NavigateTo($"/library/album/{Uri.EscapeDataString(album.Title)}");
    }

    private string GetArtUrl(string path)
    {
        return $"sonorize://albumart/?path={Uri.EscapeDataString(path)}";
    }

    public void Dispose()
    {
        LibService.LibraryChanged -= OnLibraryChanged;
    }
}