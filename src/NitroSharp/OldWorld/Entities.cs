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

        public ref RenderItemKey SortKey => ref Table.SortKeys.GetRef(Index);
        public ref RgbaFloat Color => ref Table.Colors.GetRef(Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.GetRef(Index);
        public ref SizeF Bounds => ref Table.Bounds.GetRef(Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.GetRef(Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.GetRef(Index);
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

        public ref ImageSource ImageSource => ref Table.ImageSources.GetRef(Index);

        public ref RenderItemKey SortKey => ref Table.SortKeys.GetRef(Index);
        public ref RgbaFloat Color => ref Table.Colors.GetRef(Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.GetRef(Index);
        public ref SizeF Bounds => ref Table.Bounds.GetRef(Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.GetRef(Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.GetRef(Index);
    }

    internal readonly struct TextBlock : EntityStruct<TextBlockTable>
    {
        public TextBlock(TextBlockTable table, ushort index)
        {
            Table = table;
            Index = index;
        }

        public TextBlockTable Table { get; }
        public ushort Index { get; }

        public ref Text.TextLayout Layout => ref Table.Layouts.GetRef(Index);

        public ref RenderItemKey SortKey => ref Table.SortKeys.GetRef(Index);
        public ref RgbaFloat Color => ref Table.Colors.GetRef(Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.GetRef(Index);
        public ref SizeF Bounds => ref Table.Bounds.GetRef(Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.GetRef(Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.GetRef(Index);
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

        public ref TimeSpan Duration => ref Table.Duration.GetRef(Index);
        public ref TimeSpan Elapsed => ref Table.Elapsed.GetRef(Index);
        public ref MediaClipLoopData LoopData => ref Table.LoopData.GetRef(Index);
        public ref float Volume => ref Table.Volume.GetRef(Index);
        public ref double SoundAmplitude => ref Table.SoundAmplitude.GetRef(Index);
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

        public ref TimeSpan Duration => ref Table.Duration.GetRef(Index);
        public ref TimeSpan Elapsed => ref Table.Elapsed.GetRef(Index);
        public ref MediaClipLoopData LoopData => ref Table.LoopData.GetRef(Index);
        public ref float Volume => ref Table.Volume.GetRef(Index);

        public ref RenderItemKey SortKey => ref Table.SortKeys.GetRef(Index);
        public ref RgbaFloat Color => ref Table.Colors.GetRef(Index);
        public ref BlendMode BlendMode => ref Table.BlendModes.GetRef(Index);
        public ref SizeF Bounds => ref Table.Bounds.GetRef(Index);
        public ref TransformComponents TransformComponents => ref Table.TransformComponents.GetRef(Index);
        public ref Matrix4x4 Transform => ref Table.TransformMatrices.GetRef(Index);
    }
}
