using HoppyFramework.Content;

namespace ProjectHoppy.Graphics.RenderItems
{
    public class TransitionVisual : Visual
    {
        private bool _inputsSet = false;

        public AssetRef SourceAsset { get; set; }
        public AssetRef MaskAsset { get; set; }

        public override void Render(RenderSystem renderSystem)
        {
            var content = renderSystem.Content;
            var context = renderSystem.RenderContext;

            if (content.TryGetAsset<TextureAsset>(SourceAsset, out var source)
                && content.TryGetAsset<TextureAsset>(MaskAsset, out var mask))
            {
                var effect = renderSystem.SharedResources.TransitionEffect;
                if (!_inputsSet)
                {
                    effect.SetInput(0, source, true);
                    effect.SetInput(1, mask, true);

                    _inputsSet = true;
                }

                effect.SetValue(0, Opacity);
                context.DeviceContext.DrawImage(effect);
            }
        }
    }
}
