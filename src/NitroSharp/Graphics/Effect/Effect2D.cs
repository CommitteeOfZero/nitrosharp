using System.Numerics;
using System.Runtime.CompilerServices;
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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void SetTransform(in Matrix3x2 transform)
        //{
        //    var m = Matrix4x4.Identity;
        //    m.M11 = transform.M11;
        //    m.M12 = transform.M12;
        //    m.M21 = transform.M21;
        //    m.M22 = transform.M22;
        //    m.M41 = transform.M31;
        //    m.M42 = transform.M32;
        //    Transform = m;
        //}

        public void SetOrthographicsProjection(in Viewport viewport)
        {
            Projection = Matrix4x4.CreateOrthographicOffCenter(viewport.X, viewport.X + viewport.Width,
                viewport.Y + viewport.Height, viewport.Y, 0, -1);
        }
    }
}
