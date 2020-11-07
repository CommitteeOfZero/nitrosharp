using System;
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

        public static Transform Default => new Transform { Scale = Vector3.One };

        public Matrix4x4 GetMatrix(Size? unconstrainedBounds = null)
        {
            static float rad(float deg) => deg / 180.0f * MathF.PI;

            var bounds = unconstrainedBounds?.ToVector2() ?? Vector2.Zero;
            var center = new Vector3(new Vector2(0.5f) * bounds, 0);
            var scale = Matrix4x4.CreateScale(Scale, center);
            Matrix4x4 rot = Matrix4x4.CreateRotationZ(rad(Rotation.Z), center)
                * Matrix4x4.CreateRotationY(rad(Rotation.Y), center)
                * Matrix4x4.CreateRotationX(rad(Rotation.X), center);
            var translation = Matrix4x4.CreateTranslation(Position);
            return scale * rot * translation;
        }
    }
}
