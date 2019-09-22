using System;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Text;
using Veldrid;
using NitroSharp.Interactivity;
using NitroSharp.NsScript;

namespace NitroSharp
{
    internal sealed class ThreadTable : EntityTable
    {
        public ThreadTable(OldWorld world, ushort columnCount)
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
        public Row<BlendMode> BlendModes { get; }
        public Row<SizeF> Bounds { get; }
        public Row<TransformComponents> TransformComponents { get; }

        public Row<Matrix4x4> TransformMatrices { get; }

        protected RenderItemTable(OldWorld world, ushort columnCount)
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

        public SystemDataRow<SpriteRenderer.SystemData> SystemData { get; }

        public SpriteTable(OldWorld world, ushort spriteCount)
            : base(world, spriteCount)
        {
            ImageSources = AddRow<ImageSource>();
            SystemData = AddSystemDataRow<SpriteRenderer.SystemData>();
        }

        public new Enumerator<SpriteTable, Sprite> GetEnumerator()
            => new Enumerator<SpriteTable, Sprite>(this);
    }

    internal sealed class RectangleTable : RenderItemTable
    {
        public RectangleTable(OldWorld world, ushort rectCount)
            : base(world, rectCount)
        {
        }

        public new Enumerator<RenderItemTable, RenderItem> GetEnumerator()
            => new Enumerator<RenderItemTable, RenderItem>(this);
    }

    internal sealed class TextBlockTable : RenderItemTable
    {
        public Row<TextLayout> Layouts { get; }

        public TextBlockTable(OldWorld world, ushort columnCount)
            : base(world, columnCount)
        {
            Layouts = AddRow<TextLayout>();
        }

        public new Enumerator<TextBlockTable, TextBlock> GetEnumerator()
            => new Enumerator<TextBlockTable, TextBlock>(this);
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
        public AudioClipTable(OldWorld world, ushort columnCount)
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

        public VideoClipTable(OldWorld world, ushort columnCount)
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
        public ChoiceTable(OldWorld world, ushort columnCount)
            : base(world, columnCount)
        {
            Name = AddRow<string>();
            MouseUsualSprite = AddRow<OldEntity>();
            MouseOverSprite = AddRow<OldEntity>();
            MouseClickSprite = AddRow<OldEntity>();
            MouseOverThread = AddRow<OldEntity>();
            MouseLeaveThread = AddRow<OldEntity>();
            State = AddRow<NsMouseState>();
            Rects = AddSystemDataRow<RectangleF>();
        }

        public Row<string> Name { get; }
        public Row<OldEntity> MouseUsualSprite { get; }
        public Row<OldEntity> MouseOverSprite { get; }
        public Row<OldEntity> MouseClickSprite { get; }

        public Row<OldEntity> MouseOverThread { get; }
        public Row<OldEntity> MouseLeaveThread { get; }
        public Row<NsMouseState> State { get; }

        public SystemDataRow<RectangleF> Rects { get; }
    }
}
