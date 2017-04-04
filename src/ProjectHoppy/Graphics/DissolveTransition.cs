using HoppyFramework;
using HoppyFramework.Content;

namespace ProjectHoppy.Graphics
{
    public class DissolveTransition : Component
    {
        public AssetRef Texture { get; set; }
        public AssetRef AlphaMask { get; set; }
        public float Opacity { get; set; }
    }
}
