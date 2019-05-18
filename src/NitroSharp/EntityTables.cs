using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal sealed class ThreadTable : EntityTable
    {
        public ThreadTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            Infos = AddRow<InterpreterThreadInfo>();
        }

        public Row<InterpreterThreadInfo> Infos { get; }
    }

    internal abstract class RenderItemTable : EntityTable<RenderItem>
    {
        public Row<RenderItemKey> SortKeys { get; }
        public Row<RgbaFloat> Colors { get; }
        public Row<BlendMode> BlendModes { get; }
        public Row<SizeF> Bounds { get; }
        public Row<TransformComponents> TransformComponents { get; }

        public Row<Matrix4x4> TransformMatrices { get; }
        public SystemDataRow<RectangleF> WorldRects { get; }

        protected RenderItemTable(World world, ushort columnCount)
            : base(world, columnCount)
        {
            TransformComponents = AddRow<TransformComponents>();
            SortKeys = AddRow<RenderItemKey>();
            Colors = AddRow<RgbaFloat>();
            BlendModes = AddRow<BlendMode>();
            Bounds = AddRow<SizeF>();
            TransformMatrices = AddRow<Matrix4x4>();
        }

        public Enumerator<RenderItemTable, RenderItem> GetEnumerator()
            => new Enumerator<RenderItemTable, RenderItem>(this);
    }

    internal sealed class SpriteTable : RenderItemTable
    {
        public Row<ImageSource> ImageSources { get; }

        public SpriteTable(World world, ushort spriteCount)
            : base(world, spriteCount)
        {
            ImageSources = AddRow<ImageSource>();
        }

        public new Enumerator<SpriteTable, Sprite> GetEnumerator()
            => new Enumerator<SpriteTable, Sprite>(this);
    }

    internal sealed class RectangleTable : RenderItemTable
    {
        public RectangleTable(World world, ushort rectCount)
            : base(world, rectCount)
        {
        }

        public new Enumerator<RenderItemTable, RenderItem> GetEnumerator()
            => new Enumerator<RenderItemTable, RenderItem>(this);
    }

    internal sealed class TextInstanceTable : RenderItemTable
    {
        public RefTypeRow<TextLayout> Layouts { get; }
        public Row<bool> ClearFlags { get; }

        public TextInstanceTable(World world, ushort initialCount)
            : base(world, initialCount)
        {
            Layouts = AddRefTypeRow<TextLayout>();
            ClearFlags = AddRow<bool>();
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

    internal sealed class AudioClipTable : EntityTable<AudioClip>, MediaClipTable
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
        }

        public Row<AssetId> Asset { get; }
        public Row<TimeSpan> Duration { get; }
        public Row<TimeSpan> Elapsed { get; }
        public Row<MediaClipLoopData> LoopData { get; }
        public Row<float> Volume { get; }
        public Row<double> SoundAmplitude { get; }

        public SystemDataRow<PlaybackState> PlaybackState => throw new NotImplementedException();
        public SystemDataRow<AudioState> AudioState => throw new NotImplementedException();

        public Enumerator<AudioClipTable, AudioClip> GetEnumerator()
            => new Enumerator<AudioClipTable, AudioClip>(this);
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

        public new Enumerator<VideoClipTable, VideoClip> GetEnumerator()
            => new Enumerator<VideoClipTable, VideoClip>(this);
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
