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
        public EntityStorage.ComponentVec<RenderItemKey> Keys { get; }
        public EntityStorage.ComponentVec<TransformComponents> TransformComponents { get; }
        public EntityStorage.ComponentVec<Matrix4x4> Transforms { get; }
    }

    internal interface SceneObject2DStorage : SceneObjectStorage
    {
        public EntityStorage.ComponentVec<SizeF> LocalBounds { get; }
    }

    internal abstract class RenderItemStorage : EntityStorage, SceneObjectStorage
    {
        private static uint s_id;

        public ComponentVec<RenderItemKey> Keys { get; }
        public ComponentVec<TransformComponents> TransformComponents { get; }
        public ComponentVec<Matrix4x4> Transforms { get; }

        public ComponentVec<Material> Materials { get; }
        public SystemComponentVec<DrawState> DrawState { get; }
      
        protected RenderItemStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Keys = AddComponentVec<RenderItemKey>();
            TransformComponents = AddComponentVec<TransformComponents>();
            Transforms = AddComponentVec<Matrix4x4>();
            Materials = AddComponentVec<Material>();
            DrawState = AddSystemComponentVec<DrawState>();
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
        public ComponentVec<Quad> Geometry { get; }
        public ComponentVec<SizeF> LocalBounds { get; }

        public QuadStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Geometry = AddComponentVec<Quad>();
            LocalBounds = AddComponentVec<SizeF>();
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
            Keys = AddComponentVec<RenderItemKey>();
            TransformComponents = AddComponentVec<TransformComponents>();
            Transforms = AddComponentVec<Matrix4x4>();
            LocalBounds = AddComponentVec<SizeF>();
            ImageHandles = AddComponentVec<AssetId>();
        }

        public ComponentVec<RenderItemKey> Keys { get; }
        public ComponentVec<TransformComponents> TransformComponents { get; }
        public ComponentVec<Matrix4x4> Transforms { get; }
        public ComponentVec<SizeF> LocalBounds { get; }

        public ComponentVec<AssetId> ImageHandles { get; }

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

    internal interface AbstractImageStorage
    {
        EntityStorage.ComponentVec<ImageSource> ImageSources { get; }
    }

    internal sealed class ImageStorage : EntityStorage, AbstractImageStorage
    {
        public ComponentVec<ImageSource> ImageSources { get; }

        public ImageStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            ImageSources = AddComponentVec<ImageSource>();
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
        public ComponentVec<TextLayout> Layouts { get; }
        public ComponentVec<SizeF> LocalBounds { get; }

        public TextBlockStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Layouts = AddComponentVec<TextLayout>();
            LocalBounds = AddComponentVec<SizeF>();
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
