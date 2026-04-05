using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;
using Timer = System.Timers.Timer;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class Library
{
    [Parameter] public string? AlbumName { get; set; }
    [Parameter] public string? ArtistName { get; set; }

    private string _searchQuery = "";
    private LibraryViewMode _viewMode;
    private List<Song> _filteredSongs = [];

    private SortColumn _sortColumn = SortColumn.Title;
    private bool _isAscending = true;
    private bool _isFiltering = false;

    private bool _showActionsModal = false;
    private bool _showMetadataEditor = false;
    private Song? _selectedSongForModal;
    private Song? _focusedSong;

    private readonly Timer _debounceTimer = new(300)
    {
        AutoReset = false
    };

    protected override void OnInitialized()
    {
        _viewMode = AppSettings.Library.SongsViewMode;

        _debounceTimer.Elapsed += (s, e) => InvokeAsync(ApplyFilterAndSort);
        LibService.LibraryChanged += OnLibraryChanged;
        PlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    protected override void OnParametersSet()
    {
        AlbumName = AlbumName is not null
            ? Uri.UnescapeDataString(AlbumName)
            : null;

        ArtistName = ArtistName is not null
            ? Uri.UnescapeDataString(ArtistName)
            : null;

        _ = ApplyFilterAndSort();
    }

    private void OnLibraryChanged()
    {
        InvokeAsync(() =>
        {
            _ = ApplyFilterAndSort();
            StateHasChanged();
        });
    }

    private void OnPlaybackStateChanged()
    {
        // Need to re-render so TrackTable updates the active speaker icon
        InvokeAsync(StateHasChanged);
    }

    private string GetPageTitle()
    {
        if (!string.IsNullOrEmpty(AlbumName))
        {
            return AlbumName;
        }

        if (!string.IsNullOrEmpty(ArtistName))
        {
            return ArtistName;
        }

        return "Your Library";
    }

    private void ClearFilters()
    {
        Nav.NavigateTo("/");
    }

    private void OnSearchKeyUp()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnViewModeChanged(LibraryViewMode newMode)
    {
        if (_viewMode == newMode)
        {
            return;
        }

        _viewMode = newMode;
        AppSettings.Library.SongsViewMode = newMode;
        SettingsManager.Save(AppSettings);
        StateHasChanged();
    }

    private async Task ApplyFilterAndSort()
    {
        _isFiltering = true;
        StateHasChanged();

        IReadOnlyList<Song> baseList = await LibService.SearchAsync(_searchQuery);

        _filteredSongs = await Task.Run(() =>
        {
            IEnumerable<Song> query = baseList;

            if (!string.IsNullOrEmpty(AlbumName))
            {
                query = query.Where(s => s.Album.Equals(AlbumName, StringComparison.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrEmpty(ArtistName))
            {
                query = query.Where(s => s.Artist.Equals(ArtistName, StringComparison.OrdinalIgnoreCase));
            }

            return ApplySort(query);
        });

        _isFiltering = false;
        StateHasChanged();
    }

    private List<Song> ApplySort(IEnumerable<Song> query)
    {
        return _sortColumn == SortColumn.None
            ? [.. query]
            : SortByQuery(query);
    }

    private List<Song> SortByQuery(IEnumerable<Song> query)
    {
        return _sortColumn switch
        {
            SortColumn.Title => SortByTitle(query),
            SortColumn.Artist => SortByArtist(query),
            SortColumn.Album => SortByAlbum(query),
            SortColumn.Duration => SortByDuration(query),
            _ => [.. query]
        };
    }

    private List<Song> SortByTitle(IEnumerable<Song> query)
    {
        return _isAscending
            ? [.. query.OrderBy(s => s.Title)]
            : [.. query.OrderByDescending(s => s.Title)];
    }

    private List<Song> SortByArtist(IEnumerable<Song> query)
    {
        return _isAscending
            ? [.. query.OrderBy(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Title)]
            : [.. query.OrderByDescending(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Title)];
    }

    private List<Song> SortByAlbum(IEnumerable<Song> query)
    {
        return _isAscending
            ? [.. query.OrderBy(s => s.Album).ThenBy(s => s.Title)]
            : [.. query.OrderByDescending(s => s.Album).ThenBy(s => s.Title)];
    }

    private List<Song> SortByDuration(IEnumerable<Song> query)
    {
        return _isAscending
            ? [.. query.OrderBy(s => s.Duration)]
            : [.. query.OrderByDescending(s => s.Duration)];
    }

    private void Sort(SortColumn column)
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

        _ = ApplyFilterAndSort();
    }

    private void OpenOptions(Song song)
    {
        _selectedSongForModal = song;
        _showActionsModal = true;
    }

    private void CloseOptions()
    {
        _showActionsModal = false;
    }

    private void OpenMetadataEditor(Song song)
    {
        _selectedSongForModal = song;
        _showMetadataEditor = true;
    }

    private void CloseMetadataEditor()
    {
        _showMetadataEditor = false;
        _selectedSongForModal = null;
    }

    private void GoToSettings()
    {
        Nav.NavigateTo("/settings");
    }

    private async Task OnRowClick(Song song)
    {
        _focusedSong = song;

        if (!AppSettings.Playback.PlayOnSingleClick && _viewMode != LibraryViewMode.Grid)
        {
            return;
        }

        await PlayerService.PlaySong(song, _filteredSongs);
    }

    private async Task OnRowDoubleClick(Song song)
    {
        _focusedSong = song;

        await PlayerService.PlaySong(song, _filteredSongs);
    }

    public void Dispose()
    {
        _debounceTimer.Dispose();
        LibService.LibraryChanged -= OnLibraryChanged;
        PlayerService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }
}