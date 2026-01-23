using Microsoft.AspNetCore.Components;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class PlayerBarZoneEditor
{
    [Parameter] public required string Title { get; set; }
    [Parameter] public required List<PlayerBarWidget> Widgets { get; set; }

    [Parameter] public EventCallback<int> OnDragStart { get; set; }
    [Parameter] public EventCallback<int> OnDropOnItem { get; set; } // Drop onto a specific item index
    [Parameter] public EventCallback OnDropOnZone { get; set; }      // Drop into empty space in zone
    [Parameter] public EventCallback<int> OnRemove { get; set; }
    [Parameter] public EventCallback<PlayerBarWidget> OnAdd { get; set; }

    private PlayerBarWidget? AddSelection
    {
        get;
        set
        {
            field = value;

            if (!value.HasValue)
            {
                return;
            }

            _ = OnAdd.InvokeAsync(value.Value);
            field = null;
        }
    }

    private void HandleDragStart(int index)
    {
        LayoutState.SetInternalDrag(true);
        _ = OnDragStart.InvokeAsync(index);
    }

    private void HandleDragEnd()
    {
        LayoutState.SetInternalDrag(false);
    }

    private async Task HandleItemDrop(int index)
    {
        await OnDropOnItem.InvokeAsync(index);
        LayoutState.SetInternalDrag(false);
    }

    private async Task HandleZoneDrop()
    {
        await OnDropOnZone.InvokeAsync();
        LayoutState.SetInternalDrag(false);
    }
}