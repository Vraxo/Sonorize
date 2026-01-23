using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Artists
{
    private List<ArtistGroup> _artists = [];
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
        var source = LibService.AllArtists;
        _artists = ApplySort(source);
    }

    private bool FilterArtist(ArtistGroup artist, string query)
    {
        return artist.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void Sort(string column)
    {
        if (_sortColumn == column)
        {
            _isAscending = !_isAscending;
        }
        else { _sortColumn = column; _isAscending = true; }

        LoadData();
    }

    private List<ArtistGroup> ApplySort(IEnumerable<ArtistGroup> source)
    {
        return _sortColumn switch
        {
            "Name" => _isAscending ? source.OrderBy(a => a.Name).ToList() : source.OrderByDescending(a => a.Name).ToList(),
            "Albums" => _isAscending ? source.OrderBy(a => a.AlbumCount).ToList() : source.OrderByDescending(a => a.AlbumCount).ToList(),
            "Songs" => _isAscending ? source.OrderBy(a => a.SongCount).ToList() : source.OrderByDescending(a => a.SongCount).ToList(),
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
        AppSettings.Library.ArtistsViewMode = mode;
        SettingsManager.Save(AppSettings);
        StateHasChanged();
    }

    private void OpenArtist(ArtistGroup artist)
    {
        Nav.NavigateTo($"/library/artist/{Uri.EscapeDataString(artist.Name)}");
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