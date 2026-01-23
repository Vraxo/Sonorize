using Microsoft.AspNetCore.Components;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class SettingsSlider
{
    [Parameter] public required string Label { get; set; }
    [Parameter] public int Value { get; set; }
    [Parameter] public EventCallback<int> ValueChanged { get; set; }
    [Parameter] public int Min { get; set; } = 0;
    [Parameter] public int Max { get; set; } = 100;
    [Parameter] public int Step { get; set; } = 1;
    [Parameter] public string Unit { get; set; } = ""; // Kept for API compatibility, but not displayed
    [Parameter] public bool ShowNumberInput { get; set; } = true;
    [Parameter] public int? DefaultValue { get; set; }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
        {
            await ValueChanged.InvokeAsync(val);
        }
    }

    private async Task HandleNumberChange(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int val))
        {
            val = Math.Clamp(val, Min, Max);
            await ValueChanged.InvokeAsync(val);
        }
    }

    private async Task Reset()
    {
        if (DefaultValue.HasValue)
        {
            await ValueChanged.InvokeAsync(DefaultValue.Value);
        }
    }
}