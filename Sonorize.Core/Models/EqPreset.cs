namespace Sonorize.Core.Models;

public class EqPreset
{
    public string Name { get; set; } = "New Preset";
    public bool IsDefault { get; set; } = false;
    public float[] Gains { get; set; } = new float[10];
}