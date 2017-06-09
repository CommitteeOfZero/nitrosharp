using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Content;
using CommitteeOfZero.Nitro.Foundation.Graphics;

namespace CommitteeOfZero.Nitro.Graphics
{
    public class FadeTransition : Visual
    {
        public FadeTransition()
        {
        }

        public FadeTransition(IPixelSource source, AssetRef<TextureAsset> mask)
        {
            TransitionSource = source;
            Mask = mask;
        }

        public IPixelSource TransitionSource { get; set; }
        public Visual Source { get; set; }
        public AssetRef<TextureAsset> Mask { get; set; }

        public override void Render(INitroRenderer renderer)
        {
            renderer.DrawTransition(this);
        }

        public override void OnRemoved()
        {
            Mask.Dispose();
        }

        public interface IPixelSource
        {
        }

        public struct ImageSource : IPixelSource
        {
            public ImageSource(AssetRef<TextureAsset> source)
            {
                Source = source;
            }

            public AssetRef<TextureAsset> Source { get; }
        }

        public struct SolidColorSource : IPixelSource
        {
            public SolidColorSource(RgbaValueF color)
            {
                Color = color;
            }

            public RgbaValueF Color { get; }
        }
    }
}
