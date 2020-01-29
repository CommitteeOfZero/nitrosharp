using System;
using System.Numerics;
using NitroSharp.Experimental;
using NitroSharp.Primitives;

namespace NitroSharp.Graphics
{
    internal static class TransformProcessor
    {
        public static void ProcessTransforms(World world, RenderItem2DStorage storage)
            => ProcessTransforms(world,
                storage.Entities,
                storage.LocalBounds.All,
                storage.TransformComponents.All,
                storage.Transforms.All);

        public static void ProcessTransforms(
            World world,
            ReadOnlySpan<Entity> entities,
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices)
        {
            int count = transformComponents.Length;
            for (int i = 0; i < count; i++)
            {
                Calc(entities[i], i, world, bounds, transformComponents, transformMatrices);
            }
        }

        private static void Calc(
            Entity entity,
            int index,
            World world,
            ReadOnlySpan<SizeF> bounds,
            ReadOnlySpan<TransformComponents> transformComponents,
            Span<Matrix4x4> transformMatrices)
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

            Entity parent = world.GetParent(entity);
            if (world.Exists(parent) &&
                world.GetStorage<RenderItem2DStorage>(parent) is RenderItem2DStorage storage)
            {
                int parentIdx = (int)world.LookupPointer(parent).IndexInStorage;
                {
                    Span<Matrix4x4> parentTableTransforms = storage.Transforms.All;
                    if (parentTableTransforms[parentIdx].M11 == 0)
                    {
                        Calc(parent, parentIdx, world,
                            storage.LocalBounds.All,
                            storage.TransformComponents.All,
                            parentTableTransforms);
                    }
                    worldMatrix *= parentTableTransforms[parentIdx];
                }
            }

            transformMatrices[index] = worldMatrix;
        }
    }
}
