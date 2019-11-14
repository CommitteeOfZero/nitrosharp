using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Content;
using NitroSharp.Experimental;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    [StructLayout(LayoutKind.Auto)]
    internal struct CommonItemProperties
    {
        public RenderItemKey Key;
        public RgbaFloat Color;
        public BlendMode BlendMode;
        public EffectKind Effect;
    }

    internal enum BlendMode
    {
        Alpha,
        Additive,
        Subtractive,
        Multiplicative
    }

    internal readonly struct ImageSource
    {
        public readonly AssetId ImageId;
        public readonly RectangleF SourceRectangle;

        public ImageSource(AssetId imageId, in RectangleF sourceRectangle)
        {
            ImageId = imageId;
            SourceRectangle = sourceRectangle;
        }

        public override string ToString()
            => $"{ImageId.ToString()}, {SourceRectangle.ToString()}";
    }

    internal struct TransformComponents
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3 Rotation;
    }

    internal enum EffectKind
    {
        None = 0,
        Grayscale
    }

    internal abstract class RenderItemStorage : EntityStorage
    {
        private static int s_id;

        public ComponentStorage<CommonItemProperties> CommonProperties { get; }
        public ComponentStorage<TransformComponents> TransformComponents { get; }
        public ComponentStorage<Matrix4x4> Transforms { get; }

        protected RenderItemStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            CommonProperties = AddComponentStorage<CommonItemProperties>();
            TransformComponents = AddComponentStorage<TransformComponents>();
            Transforms = AddComponentStorage<Matrix4x4>();
        }

        protected (Entity entity, uint index) New(
            EntityName name,
            int priority,
            RgbaFloat color)
        {
            (Entity e, uint i) = New(name);
            ref CommonItemProperties commonData = ref CommonProperties[i];
            commonData.Key = new RenderItemKey((ushort)priority, (ushort)++s_id);
            commonData.Color = color;
            commonData.BlendMode = BlendMode.Alpha;
            TransformComponents[i].Scale = Vector3.One;
            return (e, i);
        }
    }

    internal abstract class RenderItem2DStorage : RenderItemStorage
    {
        public ComponentStorage<SizeF> LocalBounds { get; }

        protected RenderItem2DStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            LocalBounds = AddComponentStorage<SizeF>();
        }
    }

    internal sealed class RectangleStorage : RenderItem2DStorage
    {
        public RectangleStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
        }

        public (Entity entity, uint index) New(
           EntityName name,
           SizeF size,
           int priority,
           RgbaFloat color)
        {
            (Entity e, uint i) = New(name, priority, color);
            LocalBounds[i] = size;
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

    internal sealed class SpriteStorage : RenderItem2DStorage, AbstractImageStorage
    {
        public ComponentStorage<ImageSource> ImageSources { get; }
        public SystemComponentStorage<QuadMaterial> Materials { get; }

        public SpriteStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            ImageSources = AddComponentStorage<ImageSource>();
            Materials = AddSystemComponentStorage<QuadMaterial>();
        }

        public (Entity entity, uint index) New(
            EntityName name,
            int priority,
            ImageSource src,
            RgbaFloat color,
            SizeF localBounds)
        {
            (Entity e, uint i) = New(name, priority, color);
            ImageSources[i] = src;
            LocalBounds[i] = localBounds;
            return (e, i);
        }
    }

    internal sealed class FadeTransitionStorage : RenderItem2DStorage
    {
        public FadeTransitionStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
        }
    }

    internal sealed class TextBlockStorage : RenderItem2DStorage
    {
        public ComponentStorage<TextLayout> Layouts { get; }

        public TextBlockStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Layouts = AddComponentStorage<TextLayout>();
        }

        public (Entity entity, uint index) New(
           EntityName name,
           TextLayout layout,
           int priority)
        {
            (Entity e, uint i) = New(name, priority, RgbaFloat.White);
            Layouts[i] = layout;
            return (e, i);
        }
    }
}
