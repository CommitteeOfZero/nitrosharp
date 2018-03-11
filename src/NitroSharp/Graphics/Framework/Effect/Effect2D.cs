using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics
{
    public abstract class Effect2D : Effect
    {
        protected Effect2D(GraphicsDevice graphicsDevice, Shader vertexShader, Shader fragmentShader)
            : base(graphicsDevice, vertexShader, fragmentShader)
        {
        }

        public abstract Matrix4x4 Transform { get; set; }
        public abstract Matrix4x4 Projection { get; set; }

        public void SetOrthographicsProjection(in Viewport viewport)
        {
            Projection = Matrix4x4.CreateOrthographicOffCenter(viewport.X, viewport.X + viewport.Width,
                viewport.Y + viewport.Height, viewport.Y, 0, -1);
        }
    }
}
