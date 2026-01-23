using Microsoft.AspNetCore.Components;
using Sonorize.Core;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Components.Pages;

public partial class PlaylistView
{
    [Parameter] public Guid PlaylistId { get; set; }
    private Playlist? _currentPlaylist;

    private readonly List<Song> _masterPlaylistSongs = [];
    private List<Song> _filteredSongs = [];
    private string _searchQuery = "";

    private LibraryViewMode _viewMode;

    // Sorting
    private SortColumn _sortColumn = SortColumn.None;
    private bool _isAscending = true;

    // Edit State
    private bool _isEditingName = false;
    private string _tempName = "";

    // Modal State
    private bool _showActionsModal = false;
    private bool _showMetadataEditor = false;
    private Song? _selectedSongForModal;

    private bool IsReorderAllowed =>
        string.IsNullOrWhiteSpace(_searchQuery) &&
        _sortColumn == SortColumn.None &&
        _currentPlaylist?.Type == PlaylistType.Manual &&
        _viewMode == LibraryViewMode.List;

    protected override void OnInitialized()
    {
        _viewMode = AppSettings.Library.PlaylistDetailViewMode;
        LibService.LibraryChanged += OnLibraryChanged;
    }

    protected override void OnParametersSet()
    {
        _searchQuery = "";
        _sortColumn = SortColumn.None;
        LoadPlaylist();
    }

    private void OnLibraryChanged()
    {
        _ = InvokeAsync(() =>
        {
            LoadPlaylist();
            StateHasChanged();
        });
    }

    private void ToggleViewMode()
    {
        _viewMode = _viewMode == LibraryViewMode.List ? LibraryViewMode.Grid : LibraryViewMode.List;
        AppSettings.Library.PlaylistDetailViewMode = _viewMode;
        SettingsManager.Save(AppSettings);
    }

    private void LoadPlaylist()
    {
        _currentPlaylist = LibService.AllPlaylists.FirstOrDefault(p => p.Id == PlaylistId);
        _masterPlaylistSongs.Clear();

        if (_currentPlaylist is null)
        {
            _filteredSongs.Clear();
            return;
        }

        Dictionary<string, Song> songLookup = LibService.AllSongs.ToDictionary(s => s.FilePath);
        foreach (var path in _currentPlaylist.SongFilePaths)
        {
            if (songLookup.TryGetValue(path, out var song))
            {
                _masterPlaylistSongs.Add(song);
            }
        }

        FilterPlaylist();
    }

    private void Sort(SortColumn column)
    {
        if (_sortColumn == column)
        {
            if (_sortColumn != SortColumn.None)
            {
                _isAscending = !_isAscending;
            }
        }
        else
        {
            _sortColumn = column;
            _isAscending = true;
        }

        FilterPlaylist();
    }

    private void FilterPlaylist()
    {
        IEnumerable<Song> query = _masterPlaylistSongs;

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            query = query.Where(s => s.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                     s.Artist.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                     s.Album.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (_sortColumn != SortColumn.None)
        {
            switch (_sortColumn)
            {
                case SortColumn.Title:
                    query = _isAscending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title);
                    break;
                case SortColumn.Artist:
                    query = _isAscending
                       ? query.OrderBy(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Title)
                       : query.OrderByDescending(s => s.Artist).ThenBy(s => s.Album).ThenBy(s => s.Title);
                    break;
                case SortColumn.Album:
                    query = _isAscending
                        ? query.OrderBy(s => s.Album).ThenBy(s => s.Title)
                        : query.OrderByDescending(s => s.Album).ThenBy(s => s.Title);
                    break;
                case SortColumn.Duration:
                    query = _isAscending ? query.OrderBy(s => s.Duration) : query.OrderByDescending(s => s.Duration);
                    break;
            }
        }

        _filteredSongs = query.ToList();
    }

    private void GoToSettings()
    {
        Nav.NavigateTo("/settings");
    }

    private void StartEdit()
    {
        _tempName = _currentPlaylist?.Name ?? "";
        _isEditingName = true;
    }

    private void CancelEdit()
    {
        _isEditingName = false;
    }

    private void SaveName()
    {
        if (_currentPlaylist is null || string.IsNullOrWhiteSpace(_tempName))
        {
            return;
        }

        _currentPlaylist.Name = _tempName;
        LibService.SavePlaylist(_currentPlaylist);
        _isEditingName = false;
    }

    private async Task PlayPlaylist(int startIndex)
    {
        if (_filteredSongs.Count == 0)
        {
            return;
        }

        await PlayerService.PlaySong(_filteredSongs[startIndex], _filteredSongs);
    }

    private async Task HandleGridClick(Song song)
    {
        // For grid click, we find the index in the filtered list and play from there
        int index = _filteredSongs.IndexOf(song);

        if (index == -1)
        {
            return;
        }

        await PlayPlaylist(index);
    }

    private void HandleReorder((Song Source, Song Target) args)
    {
        if (_currentPlaylist is null)
        {
            return;
        }

        int oldIndex = _masterPlaylistSongs.IndexOf(args.Source);
        int newIndex = _masterPlaylistSongs.IndexOf(args.Target);

        if (oldIndex == -1 || newIndex == -1)
        {
            return;
        }

        _masterPlaylistSongs.RemoveAt(oldIndex);
        _masterPlaylistSongs.Insert(newIndex, args.Source);

        _currentPlaylist.SongFilePaths = _masterPlaylistSongs.Select(s => s.FilePath).ToList();
        LibService.SavePlaylist(_currentPlaylist);
        FilterPlaylist();
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

    public void Dispose()
    {
        LibService.LibraryChanged -= OnLibraryChanged;
    }
}