using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Experimental;
using NitroSharp.Primitives;
using NitroSharp.Text;

#nullable enable

namespace NitroSharp.Graphics
{
    internal readonly struct ImageSource
    {
        public readonly AssetId Handle;
        public readonly RectangleF SourceRectangle;

        public ImageSource(AssetId handle, in RectangleF sourceRectangle)
        {
            Handle = handle;
            SourceRectangle = sourceRectangle;
        }

        public override string ToString()
            => $"{Handle.ToString()}, {SourceRectangle.ToString()}";
    }

    internal struct TransformComponents
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3 Rotation;
    }

    internal interface SceneObjectStorage
    {
        public ReadOnlySpan<Entity> Entities { get; }
        public EntityStorage.ComponentStorage<RenderItemKey> Keys { get; }
        public EntityStorage.ComponentStorage<TransformComponents> TransformComponents { get; }
        public EntityStorage.ComponentStorage<Matrix4x4> Transforms { get; }
    }

    internal interface SceneObject2DStorage : SceneObjectStorage
    {
        public EntityStorage.ComponentStorage<SizeF> LocalBounds { get; }
    }

    internal abstract class RenderItemStorage : EntityStorage, SceneObjectStorage
    {
        private static uint s_id;

        public ComponentStorage<RenderItemKey> Keys { get; }
        public ComponentStorage<TransformComponents> TransformComponents { get; }
        public ComponentStorage<Matrix4x4> Transforms { get; }

        public ComponentStorage<Material> Materials { get; }
        public SystemComponentStorage<DrawState> DrawState { get; }
      
        protected RenderItemStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Keys = AddComponentStorage<RenderItemKey>();
            TransformComponents = AddComponentStorage<TransformComponents>();
            Transforms = AddComponentStorage<Matrix4x4>();
            Materials = AddComponentStorage<Material>();
            DrawState = AddSystemComponentStorage<DrawState>();
        }

        protected (Entity entity, uint index) New(
           EntityName name,
           int priority)
        {
            (Entity e, uint i) = New(name);
            Keys[i] = new RenderItemKey((ushort)priority, (ushort)++s_id);
            TransformComponents[i].Scale = Vector3.One;
            return (e, i);
        }
    }

    internal sealed class QuadStorage : RenderItemStorage, SceneObject2DStorage
    {
        public ComponentStorage<QuadGeometry> Geometry { get; }
        public ComponentStorage<SizeF> LocalBounds { get; }

        public QuadStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Geometry = AddComponentStorage<QuadGeometry>();
            LocalBounds = AddComponentStorage<SizeF>();
        }

        public (Entity entity, uint index) New(
            EntityName name,
            SizeF localBounds,
            int priority,
            in Material material)
        {
            (Entity e, uint i) = base.New(name, priority);
            LocalBounds[i] = localBounds;
            Materials[i] = material;

            Entity parent = _world.GetParent(e);
            if (parent.IsValid && _world.GetStorage<EntityStorage>(parent)
                is AlphaMaskStorage maskStorage)
            {
                Materials[i].AlphaMask = maskStorage.ImageHandles[parent];
            }
            return (e, i);
        }
    }

    internal sealed class AlphaMaskStorage : EntityStorage, SceneObject2DStorage
    {
        public AlphaMaskStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Keys = AddComponentStorage<RenderItemKey>();
            TransformComponents = AddComponentStorage<TransformComponents>();
            Transforms = AddComponentStorage<Matrix4x4>();
            LocalBounds = AddComponentStorage<SizeF>();
            ImageHandles = AddComponentStorage<AssetId>();
        }

        public ComponentStorage<RenderItemKey> Keys { get; }
        public ComponentStorage<TransformComponents> TransformComponents { get; }
        public ComponentStorage<Matrix4x4> Transforms { get; }
        public ComponentStorage<SizeF> LocalBounds { get; }

        public ComponentStorage<AssetId> ImageHandles { get; }

        public (Entity entity, uint index) New(
           EntityName name,
           SizeF localBounds,
           int priority,
           AssetId imageHandle)
        {
            (Entity e, uint i) = New(name);
            Keys[i] = new RenderItemKey((ushort)priority, 0);
            TransformComponents[i].Scale = Vector3.One;
            LocalBounds[i] = localBounds;
            ImageHandles[i] = imageHandle;
            return (e, i);
        }
    }

    internal struct BarrelDistortionParameters
    {
        public AssetId DistortionMap;
    }

    internal sealed class PostEffectStorage : EntityStorage
    {
        public ComponentStorage<BarrelDistortionParameters> Parameters { get; }

        public PostEffectStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Parameters = AddComponentStorage<BarrelDistortionParameters>();
        }

        public (Entity entity, uint index) New(
            EntityName name,
            in BarrelDistortionParameters parameters)
        {
            (Entity e, uint i) = base.New(name);
            Parameters[i] = parameters;
            return (e, i);
        }
    }

    internal interface AbstractImageStorage
    {
        EntityStorage.ComponentStorage<ImageSource> ImageSources { get; }
    }

    internal sealed class ImageStorage : EntityStorage, AbstractImageStorage
    {
        public ComponentStorage<ImageSource> ImageSources { get; }

        public ImageStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            ImageSources = AddComponentStorage<ImageSource>();
        }

        public (Entity entity, uint index) New(EntityName name, ImageSource src)
        {
            (Entity e, uint i) = New(name);
            ImageSources[i] = src;
            return (e, i);
        }
    }

    internal sealed class TextBlockStorage : RenderItemStorage, SceneObject2DStorage
    {
        public ComponentStorage<TextLayout> Layouts { get; }
        public ComponentStorage<SizeF> LocalBounds { get; }

        public TextBlockStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Layouts = AddComponentStorage<TextLayout>();
            LocalBounds = AddComponentStorage<SizeF>();
        }

        public (Entity entity, uint index) New(
           EntityName name,
           TextLayout layout,
           int priority)
        {
            (Entity e, uint i) = New(name, priority);
            Layouts[i] = layout;
            return (e, i);
        }
    }
}
