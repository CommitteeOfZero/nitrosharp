﻿using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Content;
using CommitteeOfZero.NitroSharp.Foundation.Graphics;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public class FadeTransition : Visual
    {
        public FadeTransition()
        {
        }

        public FadeTransition(IPixelSource source, AssetRef<Texture2D> mask)
        {
            TransitionSource = source;
            Mask = mask;
        }

        public IPixelSource TransitionSource { get; set; }
        public Visual Source { get; set; }
        public AssetRef<Texture2D> Mask { get; set; }

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
            public ImageSource(AssetRef<Texture2D> source)
            {
                Source = source;
            }

            public AssetRef<Texture2D> Source { get; }
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