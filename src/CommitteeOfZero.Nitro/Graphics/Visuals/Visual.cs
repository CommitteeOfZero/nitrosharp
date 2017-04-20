using MoeGame.Framework;
using System;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Graphics.Visuals
{
    public abstract class Visual : Component
    {
        public Visual ParentVisual { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Scale { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }

        public RgbaValueF Color { get; set; }
        public float Opacity { get; set; }
        public int Priority { get; set; }

        public Visual()
        {
            Opacity = 1.0f;
            Scale = new Vector2(1.0f, 1.0f);
        }

        public abstract void Render(RenderSystem renderSystem);
    }
}
