using System;
using HoppyFramework;
using HoppyFramework.Graphics;
using ProjectHoppy.Graphics.RenderItems;

namespace ProjectHoppy.Graphics
{
    public enum VisualKind
    {
        Rectangle,
        Texture,
        DissolveTransition,
        Text,
        Screenshot
    }

    public class VisualComponent : Visual
    {
        public VisualComponent()
        {
        }

        public VisualComponent(VisualKind kind, float x, float y, float width, float height, int priority)
        {
            Kind = kind;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Priority = priority;
        }

        public VisualKind Kind { get; set; }

        public override void Render(DXRenderContext renderContext)
        {
        }
        //public float X { get; set; }
        //public float Y { get; set; }
        //public float Width { get; set; }
        //public float Height { get; set; }

        //public RgbaValueF Color { get; set; }
        ////public AssetRef TextureRef { get; set; }
        //public float Opacity { get; set; } = 1.0f;
        //public int Priority { get; set; }
    }
}
