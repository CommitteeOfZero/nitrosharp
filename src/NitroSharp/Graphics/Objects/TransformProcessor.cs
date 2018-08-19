using System;
using System.Numerics;
using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal static class TransformProcessor
    {
        public static void ProcessTransforms(Visuals table)
            => ProcessTransforms(
                table.Bounds.Enumerate(),
                table.TransformComponents.Enumerate(),
                table.TransformMatrices.MutateAll(),
                table.Parents.Enumerate());

        public static void ProcessTransforms(
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices,
            ReadOnlySpan<Entity> parents)
        {
            int count = transformComponents.Length;
            for (int i = 0; i < count; i++)
            {
                Entity parent = parents[i];
                if (parent.IsValid)
                {
                    CalculateRecursive(i, transformComponents, transformMatrices, bounds, parents, parent);
                }
                else
                {

                    ref readonly TransformComponents local = ref transformComponents[i];
                    SizeF size = bounds[i];
                    var center = new Vector3(new Vector2(0.5f) * new Vector2(size.Width, size.Height), 0);
                    var scale = Matrix4x4.CreateScale(local.Scale, center);
                    //var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(local.Rotation.Z), center)
                    //    * Matrix4x4.CreateRotationY(MathUtil.ToRadians(local.Rotation.Y), center)
                    //    * Matrix4x4.CreateRotationX(MathUtil.ToRadians(local.Rotation.X), center);
                    var translation = Matrix4x4.CreateTranslation(local.Position);

                    transformMatrices[i] = scale * translation;
                }
            }
        }

        private static void CalculateRecursive(int i,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices,
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<Entity> parents,
            Entity parent)
        {
            if (!parent.IsValid) { return; }

            Entity grandparent = parents[parent.Index];
            CalculateRecursive(parent.Index, transformComponents, transformMatrices, bounds, parents, grandparent);

            ref Matrix4x4 parentMatrix = ref transformMatrices[parent.Index];

            ref readonly TransformComponents local = ref transformComponents[i];
            SizeF size = bounds[i];
            var center = new Vector3(new Vector2(0.5f) * new Vector2(size.Width, size.Height), 0);
            var scale = Matrix4x4.CreateScale(local.Scale, center);
            //var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(local.Rotation.Z), center)
            //    * Matrix4x4.CreateRotationY(MathUtil.ToRadians(local.Rotation.Y), center)
            //    * Matrix4x4.CreateRotationX(MathUtil.ToRadians(local.Rotation.X), center);
            var translation = Matrix4x4.CreateTranslation(local.Position);

            transformMatrices[i] = scale * translation * parentMatrix;
        }
    }
}
