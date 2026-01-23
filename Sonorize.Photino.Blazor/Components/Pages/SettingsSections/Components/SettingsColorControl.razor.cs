using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace Sonorize.Photino.Blazor.Components.Pages.SettingsSections.Components;

public partial class SettingsColorControl
{
    [Parameter] public required string Label { get; set; }
    [Parameter] public string Value { get; set; } = "#000000";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public EventCallback OnReset { get; set; }

    private async Task HandleColorChange(ChangeEventArgs e)
    {
        string val = e.Value?.ToString() ?? "#000000";
        await UpdateValue(val);
    }

    private async Task HandleTextChange(ChangeEventArgs e)
    {
        string input = e.Value?.ToString()?.Trim() ?? "";

        // Auto-add hash if missing
        if (!input.StartsWith("#"))
        {
            input = "#" + input;
        }

        // Basic Hex Validation (6 hex digits)
        if (Regex.IsMatch(input, "^#[0-9A-Fa-f]{6}$"))
        {
            await UpdateValue(input);
        }
        else
        {
            // If invalid, force UI refresh to revert to valid Value
            StateHasChanged();
        }
    }

    private async Task UpdateValue(string val)
    {
        if (string.Equals(Value, val, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Value = val;
        await ValueChanged.InvokeAsync(val);
    }
}