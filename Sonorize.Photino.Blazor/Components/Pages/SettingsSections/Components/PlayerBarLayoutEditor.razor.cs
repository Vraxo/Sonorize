using Microsoft.AspNetCore.Components;
using Sonorize.Core.Settings;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class PlayerBarLayoutEditor
{
    [Parameter] public required PlayerBarConfig Config { get; set; }
    [Parameter] public EventCallback OnChange { get; set; }

    // Drag State
    private List<PlayerBarWidget>? _sourceList;
    private int _sourceIndex = -1;

    private void HandleDragStart(List<PlayerBarWidget> list, int index)
    {
        _sourceList = list;
        _sourceIndex = index;
    }

    private void HandleItemDrop(List<PlayerBarWidget> targetList, int targetIndex)
    {
        if (_sourceList is null || _sourceIndex == -1)
        {
            return;
        }

        var item = _sourceList[_sourceIndex];
        _sourceList.RemoveAt(_sourceIndex);

        // Adjust index if dropping into same list after the removed item
        if (_sourceList == targetList && _sourceIndex < targetIndex)
        {
            // Because removing the item shifted subsequent items up,
            // the visual target index is actually one less in the new list state.
            // However, list.Insert inserts *before*.
            // If I drop on item 5 (visually), and I was item 1.
            // Item 1 removed. Item 5 is now at index 4.
            // Insert at 4 puts me before old item 5. Correct.
            // BUT: if targetIndex was passed from the loop *before* removal, it reflects old state.
            // Let's safe guard.
            if (targetIndex > 0)
            {
                targetIndex--;
            }
        }

        // Clamp to safe bounds
        targetIndex = Math.Clamp(targetIndex, 0, targetList.Count);

        targetList.Insert(targetIndex, item);
        ResetDragState();
        NotifyChange();
    }

    private void HandleZoneDrop(List<PlayerBarWidget> targetList)
    {
        if (_sourceList is null || _sourceIndex == -1)
        {
            return;
        }

        PlayerBarWidget item = _sourceList[_sourceIndex];
        _sourceList.RemoveAt(_sourceIndex);
        targetList.Add(item);

        ResetDragState();
        NotifyChange();
    }

    private void ResetDragState()
    {
        _sourceList = null;
        _sourceIndex = -1;
    }

    private void Remove(List<PlayerBarWidget> list, int index)
    {
        list.RemoveAt(index);
        NotifyChange();
    }

    private void Add(List<PlayerBarWidget> list, PlayerBarWidget widget)
    {
        list.Add(widget);
        NotifyChange();
    }

    private void ResetDefaults()
    {
        PlayerBarConfig defaults = new();

        Config.Left = defaults.Left;
        Config.Center = defaults.Center;
        Config.Right = defaults.Right;

        NotifyChange();
    }

    private void NotifyChange()
    {
        _ = OnChange.InvokeAsync();
    }
}