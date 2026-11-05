using System.Runtime.InteropServices;
using ManagedBass;

namespace Sonorize.Core.Playback;

[StructLayout(LayoutKind.Sequential)]
public class DXParamEQParams : IEffectParameter
{
    public float fCenter;
    public float fBandwidth;
    public float fGain;

    // Correctly returning the EffectType prevents crashes if ManagedBass internals inspect this.
    public EffectType FXType => EffectType.DXParamEQ;
}
