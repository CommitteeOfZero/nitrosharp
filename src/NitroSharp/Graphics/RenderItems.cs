using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Experimental;
using NitroSharp.Interactivity;
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

    internal abstract class RenderItemStorage : EntityStorage
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
            if (name.MouseState.HasValue)
            {
                Debug.Assert(name.Parent != null);
                Entity choice = _world.GetEntity(new EntityName(name.Parent));
                ChoiceStorage? choices = _world.GetStorage<ChoiceStorage>(choice);
                if (choices != null)
                {
                    choices.AssociatedEntities[choice]
                        .SetVisualEntity(name.MouseState.Value, e);
                }
            }
            return (e, i);
        }
    }

    internal abstract class RenderItem2DStorage : RenderItemStorage
    {
        public RenderItem2DStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            LocalBounds = AddComponentVec<SizeF>();
            DesignSpaceRects = AddComponentVec<RectangleF>();
        }

        public ComponentVec<SizeF> LocalBounds { get; }
        public ComponentVec<RectangleF> DesignSpaceRects { get; }

        protected (Entity entity, uint index) New(
           EntityName name,
           int priority,
           SizeF localBounds)
        {
            (Entity e, uint i) = base.New(name, priority);
            LocalBounds[i] = localBounds;
            return (e, i);
        }
    }

    internal sealed class QuadStorage : RenderItem2DStorage
    {
        public ComponentVec<Quad> Geometry { get; }

        public QuadStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Geometry = AddComponentVec<Quad>();
        }

        public (Entity entity, uint index) New(
            EntityName name,
            SizeF localBounds,
            int priority,
            in Material material)
        {
            (Entity e, uint i) = base.New(name, priority, localBounds);
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

    internal sealed class AlphaMaskStorage : RenderItem2DStorage
    {
        public AlphaMaskStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            ImageHandles = AddComponentVec<AssetId>();
        }

        public ComponentVec<AssetId> ImageHandles { get; }

        public (Entity entity, uint index) New(
           EntityName name,
           SizeF localBounds,
           int priority,
           AssetId imageHandle)
        {
            (Entity e, uint i) = New(name, priority, localBounds);
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

    internal sealed class TextBlockStorage : RenderItem2DStorage
    {
        public ComponentVec<TextLayout> Layouts { get; }

        public TextBlockStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Layouts = AddComponentVec<TextLayout>();
        }

        public (Entity entity, uint index) New(
           EntityName name,
           TextLayout layout,
           int priority)
        {
            (Entity e, uint i) = New(name, priority, SizeF.Zero);
            Layouts[i] = layout;
            return (e, i);
        }
    }
}
