using Microsoft.AspNetCore.Components.Routing;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared;

public partial class SidebarPlaylistNav
{
    private string _playlistFilter = "";
    private bool _showCreateModal = false;
    private string _newPlaylistName = "";
    private bool _showDeleteModal = false;
    private Playlist? _playlistToDelete;

    private IEnumerable<Playlist> FilteredPlaylists => string.IsNullOrWhiteSpace(_playlistFilter)
        ? LibService.AllPlaylists
        : LibService.AllPlaylists.Where(p => p.Name.Contains(_playlistFilter, StringComparison.OrdinalIgnoreCase));

    protected override void OnInitialized()
    {
        Nav.LocationChanged += OnLocationChanged;
        LibService.LibraryChanged += OnLibraryChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnLibraryChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void NavigateTo(string url)
    {
        Nav.NavigateTo(url);
    }

    private string GetActiveState(string href)
    {
        string currentUri = Nav.Uri;
        string targetUri = Nav.ToAbsoluteUri(href).ToString();

        return currentUri.Equals(targetUri, StringComparison.OrdinalIgnoreCase)
            ? "active"
            : "";
    }

    private void ClearFilter()
    {
        _playlistFilter = "";
    }

    private void OpenCreateModal()
    {
        _newPlaylistName = "";
        _showCreateModal = true;
    }

    private void CloseCreateModal()
    {
        _showCreateModal = false;
    }

    private void ConfirmCreatePlaylist()
    {
        if (string.IsNullOrWhiteSpace(_newPlaylistName))
        {
            return;
        }

        Playlist newPlaylist = LibService.CreatePlaylist(_newPlaylistName);
        _showCreateModal = false;

        Nav.NavigateTo($"playlist/{newPlaylist.Id}");
    }

    private void PromptDeletePlaylist(Playlist playlist)
    {
        _playlistToDelete = playlist;
        _showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        _showDeleteModal = false;
        _playlistToDelete = null;
    }

    private void ConfirmDeletePlaylist()
    {
        if (_playlistToDelete is not null)
        {
            LibService.DeletePlaylist(_playlistToDelete);

            if (Nav.Uri.EndsWith($"playlist/{_playlistToDelete.Id}"))
            {
                Nav.NavigateTo("/");
            }
        }

        CloseDeleteModal();
    }

    public void Dispose()
    {
        Nav.LocationChanged -= OnLocationChanged;
        LibService.LibraryChanged -= OnLibraryChanged;
        GC.SuppressFinalize(this);
    }
}