using Microsoft.AspNetCore.Components;

namespace Sonorize.Photino.Blazor.Shared.Modals;

public partial class EqFaders
{
    [Parameter] public float[] Gains { get; set; } = new float[10];
    [Parameter] public EventCallback<(int Index, float Value)> OnGainChanged { get; set; }

    private readonly string[] _labels =
    [
        "Sub",
        "Bass",
        "Low",
        "Body",
        "Mid",
        "Voc",
        "Pres",
        "Def",
        "High",
        "Air"
    ];

    private readonly string[] _tooltips =
    [
        "31 Hz",
        "63 Hz",
        "125 Hz",
        "250 Hz",
        "500 Hz",
        "1 kHz",
        "2 kHz",
        "4 kHz",
        "8 kHz",
        "16 kHz"
    ];

    private async Task HandleInput(int index, ChangeEventArgs e)
    {
        if (!float.TryParse(e.Value?.ToString(), out float val))
        {
            return;
        }

        await OnGainChanged.InvokeAsync((index, val));
    }
}