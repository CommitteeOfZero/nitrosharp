using System.Numerics;
using System.Runtime.InteropServices;

#nullable enable

namespace NitroSharp.Graphics
{
    [StructLayout(LayoutKind.Auto)]
    internal struct Transform
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3 Rotation;

        public Vector2 AnchorPoint;
        public bool Inherit;

        public Transform(bool inherit) : this()
        {
            Inherit = inherit;
            Scale = Vector3.One;
        }

        //public static void Update(World world, RenderItem3D renderItem)
        //{
        //    ref Transform transform = ref renderItem.Transform;
        //    transform.Matrix = Matrix4x4.CreateScale(transform.Scale)
        //        * Matrix4x4.CreateTranslation(transform.Position);
        //}

        public Matrix4x4 GetMatrix(SizeF unconstrainedBounds)
        {
            Vector2 bounds = unconstrainedBounds.ToVector();
            var center = new Vector3(new Vector2(0.5f) * bounds, 0);
            var scale = Matrix4x4.CreateScale(Scale, center);

            //var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(local.Rotation.Z), center)
            //    * Matrix4x4.CreateRotationY(MathUtil.ToRadians(local.Rotation.Y), center)
            //    * Matrix4x4.CreateRotationX(MathUtil.ToRadians(local.Rotation.X), center);

            Vector3 finalPos = Position - new Vector3(bounds * AnchorPoint, 0);
            var translation = Matrix4x4.CreateTranslation(finalPos);

            return scale * translation;

            //transform.Matrix = scale * translation;
            //if (transform.Inherit)
            //{
            //    transform.Matrix *= parentTransform;
            //}
            //foreach (EntityId child in renderItem.Children)
            //{
            //    if (world.Get(child) is RenderItem2D childItem)
            //    {
            //        Calculate(ctx, world, childItem, transform.Matrix);
            //    }
            //}
        }
    }
}
