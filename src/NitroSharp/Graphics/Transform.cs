using System.Numerics;
using System.Runtime.InteropServices;

#nullable enable

namespace NitroSharp.Graphics
{
    [StructLayout(LayoutKind.Auto)]
    internal struct Transform
    {
        public bool Inherit;
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3 Rotation;

        public Matrix4x4 Matrix;

        public Transform(bool inherit) : this()
        {
            Inherit = inherit;
            Scale = Vector3.One;
        }

        public void Calc(World world, RenderItem2D renderItem)
        {
            SizeF size = renderItem.Bounds;
            var center = new Vector3(new Vector2(0.5f) * new Vector2(size.Width, size.Height), 0);
            var scale = Matrix4x4.CreateScale(Scale, center);
            //var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(local.Rotation.Z), center)
            //    * Matrix4x4.CreateRotationY(MathUtil.ToRadians(local.Rotation.Y), center)
            //    * Matrix4x4.CreateRotationX(MathUtil.ToRadians(local.Rotation.X), center);
            var translation = Matrix4x4.CreateTranslation(Position);

            Matrix4x4 worldMatrix = scale * translation;
            if (world.Get(renderItem.Parent) is RenderItem2D parent)
            {
                Calc(world, parent);
                if (Inherit)
                {
                    worldMatrix *= parent.Transform.Matrix;
                }
            }

            Matrix = worldMatrix;
        }
    }
}
