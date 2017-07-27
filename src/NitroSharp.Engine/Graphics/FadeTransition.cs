﻿using NitroSharp.Foundation;
using NitroSharp.Foundation.Content;
using NitroSharp.Foundation.Graphics;
using System;
using System.Drawing;

namespace NitroSharp.Graphics
{
    public class FadeTransition : Visual
    {
        private readonly SizeF _size;

        public FadeTransition()
        {
        }

        public FadeTransition(IPixelSource source, AssetRef<Texture2D> mask)
        {
            TransitionSource = source;
            Mask = mask;
            _size = mask.Asset.Size;
        }

        public IPixelSource TransitionSource { get; set; }
        public AssetRef<Texture2D> Mask { get; set; }

        public override void Render(INitroRenderer renderer)
        {
            renderer.DrawTransition(this);
        }

        public override void OnRemoved()
        {
            Mask.Dispose();
            TransitionSource.Dispose();
        }

        public override SizeF Measure() => _size;

        public override void Free(INitroRenderer nitroRenderer)
        {
            nitroRenderer.Free(this);
        }

        public interface IPixelSource : IDisposable
        {
        }

        public struct ImageSource : IPixelSource
        {
            public ImageSource(AssetRef<Texture2D> source)
            {
                Source = source;
            }

            public AssetRef<Texture2D> Source { get; }

            public void Dispose()
            {
                Source.Dispose();
            }
        }

        public struct SolidColorSource : IPixelSource
        {
            public SolidColorSource(RgbaValueF color)
            {
                Color = color;
            }

            public RgbaValueF Color { get; }

            public void Dispose()
            {
            }
        }
    }
}
