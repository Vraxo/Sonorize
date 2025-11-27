using ManagedBass;
using System.Runtime.InteropServices;

namespace Sonorize.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
public class DXParamEQParams : IEffectParameter
{
    public float fCenter;
    public float fBandwidth;
    public float fGain;

    // Correctly returning the EffectType prevents crashes if ManagedBass internals inspect this.
    public EffectType FXType => EffectType.DXParamEQ;
}