using Microsoft.AspNetCore.Components;
using Sonorize.Core.Models;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class EqPresetBar
{
    [Parameter] public bool IsEnabled { get; set; }
    [Parameter] public EventCallback<bool> IsEnabledChanged { get; set; }

    [Parameter] public string CurrentPresetName { get; set; } = "";
    [Parameter] public IEnumerable<EqPreset> UserPresets { get; set; } = [];
    [Parameter] public IEnumerable<EqPreset> DefaultPresets { get; set; } = [];

    [Parameter] public bool CanDelete { get; set; }

    [Parameter] public EventCallback<string> OnPresetSelected { get; set; }
    [Parameter] public EventCallback<string> OnSave { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }

    private string _saveName = "";

    private async Task OnToggle(ChangeEventArgs e)
    {
        bool val = (bool)(e.Value ?? false);
        await IsEnabledChanged.InvokeAsync(val);
    }

    private async Task OnSelectChange(ChangeEventArgs e)
    {
        string? val = e.Value?.ToString();

        if (string.IsNullOrEmpty(val))
        {
            return;
        }

        await OnPresetSelected.InvokeAsync(val);
    }

    private async Task HandleSave()
    {
        if (string.IsNullOrWhiteSpace(_saveName))
        {
            return;
        }

        await OnSave.InvokeAsync(_saveName);
        _saveName = "";
    }

    private async Task HandleDelete()
    {
        await OnDelete.InvokeAsync();
    }
}