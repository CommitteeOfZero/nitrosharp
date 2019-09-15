using System;
using System.Numerics;
using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal static class TransformProcessor
    {
        public static void ProcessTransforms(World world, RenderItemTable table)
            => ProcessTransforms(world,
                table.Bounds.Enumerate(),
                table.TransformComponents.Enumerate(),
                table.TransformMatrices.MutateAll(),
                table.Parents.Enumerate());

        public static void ProcessTransforms(
            World world,
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices,
            ReadOnlySpan<Entity> parents)
        {
            int count = transformComponents.Length;
            for (int i = 0; i < count; i++)
            {
                Calc(i, world, bounds, transformComponents, transformMatrices, parents);
            }
        }

        private static void Calc(
            int index,
            World world,
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices,
            ReadOnlySpan<Entity> parents)
        {
            ref readonly TransformComponents local = ref transformComponents[index];
            SizeF size = bounds[index];
            var center = new Vector3(new Vector2(0.5f) * new Vector2(size.Width, size.Height), 0);
            var scale = Matrix4x4.CreateScale(local.Scale, center);
            //var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(local.Rotation.Z), center)
            //    * Matrix4x4.CreateRotationY(MathUtil.ToRadians(local.Rotation.Y), center)
            //    * Matrix4x4.CreateRotationX(MathUtil.ToRadians(local.Rotation.X), center);
            var translation = Matrix4x4.CreateTranslation(local.Position);

            Matrix4x4 worldMatrix = scale * translation;

            Entity parent = parents[index];
            if (parent.IsValid && parent.IsVisual)
            {
                RenderItemTable table = world.GetTable<RenderItemTable>(parent);
                if (table.TryLookupIndex(parent, out ushort parentIdx))
                {
                    Span<Matrix4x4> parentTableTransforms = table.TransformMatrices.MutateAll();
                    if (parentTableTransforms[parentIdx].M11 == 0)
                    {
                        Calc(parentIdx, world,
                            table.Bounds.Enumerate(),
                            table.TransformComponents.Enumerate(),
                            parentTableTransforms,
                            table.Parents.Enumerate());
                    }
                    worldMatrix *= parentTableTransforms[parentIdx];
                }
            }

            transformMatrices[index] = worldMatrix;
        }
    }
}
