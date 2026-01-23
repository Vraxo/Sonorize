using Microsoft.AspNetCore.Components;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class MetadataEditorModal
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public Song? Song { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private SongMetadata? _meta;
    private bool _isLoading = false;
    private bool _loadError = false;
    private bool _isSaving = false;
    private string? _saveError;

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && Song is not null)
        {
            await LoadMetadata();
        }
        else if (!IsVisible)
        {
            // Reset state when closed
            _meta = null;
            _saveError = null;
            _loadError = false;
        }
    }

    private async Task LoadMetadata()
    {
        if (Song is null)
        {
            return;
        }

        _isLoading = true;
        _loadError = false;

        // Offload to thread to avoid blocking UI on slow disk I/O
        _meta = await Task.Run(() => LibService.GetSongMetadata(Song));

        _isLoading = false;
        if (_meta is null)
        {
            _loadError = true;
        }
    }

    private async Task Save()
    {
        if (Song is null || _meta is null)
        {
            return;
        }

        _isSaving = true;
        _saveError = null;

        bool success = await Task.Run(() => LibService.UpdateSongMetadata(Song, _meta));

        _isSaving = false;

        if (success)
        {
            await OnClose.InvokeAsync();
        }
        else
        {
            _saveError = "Failed to save. File may be in use or read-only.";
        }
    }

    private async Task Cancel()
    {
        await OnClose.InvokeAsync();
    }
}