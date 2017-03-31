using SharpDX.Direct2D1;

namespace ProjectHoppy.Graphics.Effects
{
    [CustomEffect("Sample Effect", "General", "SomeAnonDev")]
    [CustomEffectInput("Texture")]
    [CustomEffectInput("Mask")]
    public class DissolveEffect : CustomEffectBase
    {
        public override void Initialize(EffectContext effectContext, TransformGraph transformGraph)
        {

        }
    }
}
