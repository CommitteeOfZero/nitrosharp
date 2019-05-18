using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp
{
    internal readonly struct RenderItem : EntityStruct<RenderItemTable>
    {
        public RenderItem(RenderItemTable table, ushort index)
        {
            Table = table;
            Index = index;
        }

        public RenderItemTable Table { get; }
        public ushort Index { get; }

        public ref readonly RenderItemKey SortKey => ref Table.SortKeys.GetValue(Index);
        public ref readonly RgbaFloat Color => ref Table.Colors.GetValue(Index);
        public ref readonly BlendMode BlendMode => ref Table.BlendModes.GetValue(Index);
        public ref readonly SizeF Bounds => ref Table.Bounds.GetValue(Index);
        public ref readonly TransformComponents TransformComponents => ref Table.TransformComponents.GetValue(Index);
        public ref readonly Matrix4x4 Transform => ref Table.TransformMatrices.GetValue(Index);

        public MutableRenderItem AsMutable() => new MutableRenderItem(Table, Index);
    }

    internal readonly struct MutableRenderItem : MutableEntityStruct<RenderItemTable>
    {
        public MutableRenderItem(RenderItemTable table, ushort index)
        {
            Table = table;
            Index = index;
            Entity = table.Entities[index];
        }

        public RenderItemTable Table { get; }
        public Entity Entity { get; }
        public ushort Index { get; }

        public ref RenderItemKey SortKey => ref Table.SortKeys.Mutate(Entity.Id, Index);
        public ref RgbaFloat Color => ref Table.Colors.Mutate(Entity.Id, Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.Mutate(Entity.Id, Index);
        public ref SizeF Bounds => ref Table.Bounds.Mutate(Entity.Id, Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.Mutate(Entity.Id, Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.Mutate(Entity.Id, Index);
    }

    internal readonly struct Sprite : EntityStruct<SpriteTable>
    {
        public Sprite(SpriteTable table, ushort index)
        {
            Table = table;
            Index = index;
        }

        public SpriteTable Table { get; }
        public ushort Index { get; }

        public ref readonly ImageSource ImageSource => ref Table.ImageSources.GetValue(Index);

        public ref readonly RenderItemKey SortKey => ref Table.SortKeys.GetValue(Index);
        public ref readonly RgbaFloat Color => ref Table.Colors.GetValue(Index);
        public ref readonly BlendMode BlendMode => ref Table.BlendModes.GetValue(Index);
        public ref readonly SizeF Bounds => ref Table.Bounds.GetValue(Index);
        public ref readonly TransformComponents TransformComponents => ref Table.TransformComponents.GetValue(Index);
        public ref readonly Matrix4x4 Transform => ref Table.TransformMatrices.GetValue(Index);

        public MutableSprite AsMutable() => new MutableSprite(Table, Index);
    }

    internal readonly struct MutableSprite : MutableEntityStruct<SpriteTable>
    {
        public MutableSprite(SpriteTable table, ushort index)
        {
            Table = table;
            Index = index;
            Entity = table.Entities[index];
        }

        public SpriteTable Table { get; }
        public Entity Entity { get; }
        public ushort Index { get; }

        public ref ImageSource ImageSource => ref Table.ImageSources.Mutate(Entity.Id, Index);

        public ref RenderItemKey SortKey => ref Table.SortKeys.Mutate(Entity.Id, Index);
        public ref RgbaFloat Color => ref Table.Colors.Mutate(Entity.Id, Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.Mutate(Entity.Id, Index);
        public ref SizeF Bounds => ref Table.Bounds.Mutate(Entity.Id, Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.Mutate(Entity.Id, Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.Mutate(Entity.Id, Index);
    }

    internal readonly struct AudioClip : EntityStruct<AudioClipTable>
    {
        public AudioClip(AudioClipTable table, ushort index)
        {
            Table = table;
            Index = index;
        }

        public AudioClipTable Table { get; }
        public ushort Index { get; }

        public ref readonly TimeSpan Duration => ref Table.Duration.GetValue(Index);
        public ref readonly TimeSpan Elapsed => ref Table.Duration.GetValue(Index);
        public ref readonly MediaClipLoopData LoopData => ref Table.LoopData.GetValue(Index);
        public ref readonly float Volume => ref Table.Volume.GetValue(Index);
        public ref readonly double SoundAmplitude => ref Table.SoundAmplitude.GetValue(Index);

        public MutableAudioClip AsMutable => new MutableAudioClip(Table, Index);
    }

    internal readonly struct MutableAudioClip : MutableEntityStruct<AudioClipTable>
    {
        public MutableAudioClip(AudioClipTable table, ushort index)
        {
            Table = table;
            Index = index;
            Entity = table.Entities[index];
        }

        public AudioClipTable Table { get; }
        public Entity Entity { get; }
        public ushort Index { get; }

        public ref TimeSpan Duration => ref Table.Duration.Mutate(Entity.Id, Index);
        public ref TimeSpan Elapsed => ref Table.Duration.Mutate(Entity.Id, Index);
        public ref MediaClipLoopData LoopData => ref Table.LoopData.Mutate(Entity.Id, Index);
        public ref float Volume => ref Table.Volume.Mutate(Entity.Id, Index);
    }

    internal readonly struct VideoClip : EntityStruct<VideoClipTable>
    {
        public VideoClip(VideoClipTable table, ushort index)
        {
            Table = table;
            Index = index;
        }

        public VideoClipTable Table { get; }
        public ushort Index { get; }

        public ref readonly TimeSpan Duration => ref Table.Duration.GetValue(Index);
        public ref readonly TimeSpan Elapsed => ref Table.Duration.GetValue(Index);
        public ref readonly MediaClipLoopData LoopData => ref Table.LoopData.GetValue(Index);
        public ref readonly float Volume => ref Table.Volume.GetValue(Index);

        public ref readonly RenderItemKey SortKey => ref Table.SortKeys.GetValue(Index);
        public ref readonly RgbaFloat Color => ref Table.Colors.GetValue(Index);
        public ref readonly BlendMode BlendMode => ref Table.BlendModes.GetValue(Index);
        public ref readonly SizeF Bounds => ref Table.Bounds.GetValue(Index);
        public ref readonly TransformComponents TransformComponents => ref Table.TransformComponents.GetValue(Index);
        public ref readonly Matrix4x4 Transform => ref Table.TransformMatrices.GetValue(Index);

        public MutableVideoClip AsMutable() => new MutableVideoClip(Table, Index);
    }

    internal readonly struct MutableVideoClip : MutableEntityStruct<VideoClipTable>
    {
        public MutableVideoClip(VideoClipTable table, ushort index)
        {
            Table = table;
            Index = index;
            Entity = table.Entities[index];
        }

        public VideoClipTable Table { get; }
        public Entity Entity { get; }
        public ushort Index { get; }

        public ref TimeSpan Duration => ref Table.Duration.Mutate(Entity.Id, Index);
        public ref TimeSpan Elapsed => ref Table.Duration.Mutate(Entity.Id, Index);
        public ref MediaClipLoopData LoopData => ref Table.LoopData.Mutate(Entity.Id, Index);
        public ref float Volume => ref Table.Volume.Mutate(Entity.Id, Index);

        public ref RenderItemKey SortKey => ref Table.SortKeys.Mutate(Entity.Id, Index);
        public ref RgbaFloat Color => ref Table.Colors.Mutate(Entity.Id, Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.Mutate(Entity.Id, Index);
        public ref SizeF Bounds => ref Table.Bounds.Mutate(Entity.Id, Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.Mutate(Entity.Id, Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.Mutate(Entity.Id, Index);
    }
}
