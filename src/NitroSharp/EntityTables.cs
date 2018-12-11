using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.Graphics.Renderers;
using NitroSharp.Media;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal enum EntityKind : ushort
    {
        Thread,
        Sprite,
        Rectangle,
        Text,
        AudioClip,
        VideoClip,
        Choice
    }

    internal sealed class ThreadTable : EntityTable
    {
        public ThreadTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            Infos = AddRow<InterpreterThreadInfo>();
        }

        public Row<InterpreterThreadInfo> Infos { get; }
    }

    internal abstract class RenderItemTable : EntityTable
    {
        public Row<RenderItemKey> SortKeys { get; }
        public Row<RgbaFloat> Colors { get; }
        public Row<SizeF> Bounds { get; }
        public Row<TransformComponents> TransformComponents { get; }

        public SystemDataRow<Matrix4x4> TransformMatrices { get; }
        public SystemDataRow<RectangleF> WorldRects { get; }

        public RenderItemTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            TransformComponents = AddRow<TransformComponents>();
            SortKeys = AddRow<RenderItemKey>();
            Colors = AddRow<RgbaFloat>();
            Bounds = AddRow<SizeF>();

            TransformMatrices = AddSystemDataRow<Matrix4x4>();
            WorldRects = AddSystemDataRow<RectangleF>();
        }
    }

    internal sealed class SpriteTable : RenderItemTable
    {
        public Row<ImageSource> ImageSources { get; }
        public SystemDataRow<SpriteSystemData> SystemData { get; }

        public SpriteTable(World world, ushort spriteCount)
            : base(world, spriteCount)
        {
            ImageSources = AddRow<ImageSource>();
            SystemData = AddSystemDataRow<SpriteSystemData>();
        }
    }

    internal sealed class RectangleTable : RenderItemTable
    {
        public RectangleTable(World world, ushort rectCount)
            : base(world, rectCount)
        {
        }
    }

    internal sealed class TextInstanceTable : RenderItemTable
    {
        public RefTypeRow<TextLayout> Layouts { get; }
        public SystemDataRow<bool> ClearFlags { get; }
        public SystemDataRow<TextSystemData> SystemData { get; }

        public TextInstanceTable(World world, ushort initialCount)
            : base(world, initialCount)
        {
            Layouts = AddRefTypeRow<TextLayout>();
            ClearFlags = AddSystemDataRow<bool>();
            SystemData = AddSystemDataRow<TextSystemData>();
        }
    }

    internal interface MediaClipTable
    {
        EntityTable.Row<AssetId> Asset { get; }
        EntityTable.Row<TimeSpan> Duration { get; }
        EntityTable.Row<TimeSpan> Elapsed { get; }
        EntityTable.Row<MediaClipLoopData> LoopData { get; }
        EntityTable.Row<float> Volume { get; }

        EntityTable.SystemDataRow<PlaybackState> PlaybackState { get; }
        EntityTable.SystemDataRow<AudioState> AudioState { get; }
    }

    internal sealed class AudioClipTable : EntityTable, MediaClipTable
    {
        public AudioClipTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            Asset = AddRow<AssetId>();
            Duration = AddRow<TimeSpan>();
            Elapsed = AddRow<TimeSpan>();
            LoopData = AddRow<MediaClipLoopData>();
            Volume = AddRow<float>();
            SoundAmplitude = AddRow<double>();
            PlaybackState = AddSystemDataRow<PlaybackState>();
            AudioState = AddSystemDataRow<AudioState>();
        }

        public Row<AssetId> Asset { get; }
        public Row<TimeSpan> Duration { get; }
        public Row<TimeSpan> Elapsed { get; }
        public Row<MediaClipLoopData> LoopData { get; }
        public Row<float> Volume { get; }
        public Row<double> SoundAmplitude { get; }

        public SystemDataRow<PlaybackState> PlaybackState { get; }
        public SystemDataRow<AudioState> AudioState { get; }
    }

    internal sealed class VideoClipTable : RenderItemTable, MediaClipTable
    {
        public Row<AssetId> Asset { get; }
        public Row<TimeSpan> Duration { get; }
        public Row<TimeSpan> Elapsed { get; }
        public Row<MediaClipLoopData> LoopData { get; }
        public Row<float> Volume { get; }

        public SystemDataRow<PlaybackState> PlaybackState { get; }
        public SystemDataRow<VideoState> VideoState { get; }
        public SystemDataRow<AudioState> AudioState { get; }

        public VideoClipTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            Asset = AddRow<AssetId>();
            Duration = AddRow<TimeSpan>();
            Elapsed = AddRow<TimeSpan>();
            LoopData = AddRow<MediaClipLoopData>();
            Volume = AddRow<float>();
            PlaybackState = AddSystemDataRow<PlaybackState>();
            VideoState = AddSystemDataRow<VideoState>();
            AudioState = AddSystemDataRow<AudioState>();
        }
    }

    internal sealed class ChoiceTable : EntityTable
    {
        public ChoiceTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            Name = AddRefTypeRow<string>();
            MouseUsualSprite = AddRow<Entity>();
            MouseOverSprite = AddRow<Entity>();
            MouseClickSprite = AddRow<Entity>();
            MouseOverThread = AddRow<Entity>();
            MouseLeaveThread = AddRow<Entity>();
            State = AddRow<Interactivity.State>();
            Rects = AddSystemDataRow<RectangleF>();
        }

        public RefTypeRow<string> Name { get; }
        public Row<Entity> MouseUsualSprite { get; }
        public Row<Entity> MouseOverSprite { get; }
        public Row<Entity> MouseClickSprite { get; }

        public Row<Entity> MouseOverThread { get; }
        public Row<Entity> MouseLeaveThread { get; }
        public Row<Interactivity.State> State { get; }

        public SystemDataRow<RectangleF> Rects { get; }
    }
}
