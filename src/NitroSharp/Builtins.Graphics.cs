using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.VM;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal partial class Builtins
    {
        public override void CreateChoice(in EntityPath entityPath)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new Choice(resolvedPath));
            }
        }

        public override void SetNextFocus(
            in EntityPath first,
            in EntityPath second,
            NsFocusDirection focusDirection)
        {
            if (_world.Get(first) is RenderItem2D { Parent: Choice choiceA }
                && _world.Get(second) is RenderItem2D { Parent: Choice choiceB })
            {
                choiceA.SetNextFocus(focusDirection, choiceB.Id);
            }
        }

        public override bool UpdateChoice(in EntityPath choicePath)
        {
            if (_world.Get(choicePath) is Choice choice)
            {
                return choice.Update();
            }

            return false;
        }

        public override void LoadColor(
            in EntityPath entityPath,
            uint width, uint height,
            NsColor color)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new ColorSource(
                    resolvedPath,
                    color.ToRgbaFloat(),
                    new Size(width, height)
                ));
            }
        }

        public override void LoadImage(in EntityPath entityPath, string fileName)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.RequestTexture(fileName) is AssetRef<Texture> texture)
            {
                _world.Add(new Image(resolvedPath, texture));
            }
        }

        public override void CreateRectangle(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint width, uint height,
            NsColor color)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new Sprite(
                    resolvedPath, priority,
                    SpriteTexture.SolidColor(color.ToRgbaFloat(), new Size(width, height))
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override void CreateSprite(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            string source)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source) is SpriteTexture texture)
            {
                _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    texture
                ).WithPosition(_renderCtx, x, y));

                if (texture.Kind == SpriteTextureKind.StandaloneTexture)
                {
                    Debug.Assert(texture.Standalone is object);
                    _ctx.Wait(
                        CurrentThread,
                        WaitCondition.FrameReady,
                        timeout: null,
                        entityQuery: null,
                        texture.Standalone
                    );
                }
            }
        }

        private SpriteTexture? ResolveSpriteSource(string src, in RectangleU? srcRect = null)
        {
            if (src == "SCREEN")
            {
                return SpriteTexture.FromStandalone(_renderCtx.CreateFullscreenTexture());
            }

            Entity? srcEntity = _world.Get(new EntityPath(src));
            if (srcEntity is ColorSource colorSrc)
            {
                return SpriteTexture.SolidColor(colorSrc.Color, colorSrc.Size);
            }

            AssetRef<Texture>? assetRefOpt = srcEntity is Image img
                ? img.Texture.Clone()
                : _ctx.Content.RequestTexture(src);
            return assetRefOpt is {} assetRef
                ? SpriteTexture.FromAsset(assetRef, srcRect)
                : (SpriteTexture?)null;
        }

        public override void CreateSpriteEx(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint srcX, uint srcY,
            uint width, uint height,
            string source)
        {
            var srcRect = new RectangleU(srcX, srcY, width, height);
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source, srcRect) is SpriteTexture texture)
            {
                _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    texture
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override void CreateTextBlock(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            NsTextDimension width, NsTextDimension height,
            string pxmlText)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                Vector4 padding = resolvedPath.Parent is null
                    ? new Vector4(0, 4, 0, 20)
                    : new Vector4(0, 16, 0, 16);

                var textBuffer = TextBuffer.FromPXmlString(pxmlText, _ctx.FontConfig);

                uint w = width is { Variant: NsTextDimensionVariant.Value, Value: {} sWidth }
                    ? (uint)sWidth : 0;
                uint h = height  is { Variant: NsTextDimensionVariant.Value, Value: {} sHeight }
                    ? (uint)sHeight : 0;
                Size? innerBounds = (w, h) is (0, 0)
                    ? (Size?)null
                    : new Size(w - (uint)(padding.X + padding.Z), h - (uint)(padding.Y + padding.W));

                if (textBuffer.AssertSingleTextSegment() is TextSegment textSegment)
                {
                    var layout = new TextLayout(
                        _ctx.GlyphRasterizer,
                        textSegment.TextRuns.AsSpan(),
                        innerBounds
                    );
                    _world.Add(new TextBlock(
                        resolvedPath,
                        _renderCtx.Text,
                        priority,
                        layout,
                        padding
                    ).WithPosition(_renderCtx, x, y));
                }
            }
        }

        public override void SetFont(
            string family, int size,
            NsColor color, NsColor outlineColor,
            NsFontWeight weight,
            NsOutlineOffset outlineOffset)
        {
            static int mapFontSize(int size) => size switch
            {
                23 => 22,
                26 => 25,
                _ => size
            };

            _ctx.FontConfig
                .WithDefaultSize(new PtFontSize(mapFontSize(size)))
                .WithDefaultColor(color.ToRgbaFloat());
        }

        public override void CreateDialogueBox(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint width, uint height,
            bool inheritTransform)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new DialogueBox(
                    resolvedPath,
                    priority,
                    new Size(width, height),
                    inheritTransform
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override void LoadDialogue(
            in DialogueBlockToken blockToken,
            uint maxWidth, uint maxHeight,
            int letterSpacing, int lineSpacing)
        {
            var path = new EntityPath($"{blockToken.BoxName}/{blockToken.BlockName}");
            if (_world.ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                _ctx.DialogueThread = _ctx.VM.ActivateDialogueBlock(blockToken);
                _ctx.CurrentDialoguePage = _world.Add(new DialoguePage(
                    resolvedPath,
                    int.MaxValue,
                    new Size(maxWidth, maxHeight),
                    lineSpacing,
                    _renderCtx.GlyphRasterizer,
                    _ctx.DialogueThread
                )).WithPosition(_renderCtx, default, default);
            }
        }

        public override void DisplayLine(in EntityPath dialogueBlockPath, string line)
        {
            if (_world.Get(dialogueBlockPath) is DialoguePage page)
            {
                var buffer = TextBuffer.FromPXmlString(line, _ctx.FontConfig);
                page.Load(buffer);
            }
        }

        public override void WaitText(in EntityQuery query, TimeSpan timeout)
        {
            var q = new EntityQuery(_ctx.CurrentDialoguePage!.Id.Path);
            _ctx.Wait(CurrentThread, WaitCondition.EntityIdle, null, q);
        }

        public override void CreateAlphaMask(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            string imagePath,
            bool inheritTransform)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.RequestTexture(imagePath) is AssetRef<Texture> texture)
            {
                _world.Add(new AlphaMask(
                    resolvedPath,
                    priority,
                    texture,
                    inheritTransform
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override int GetWidth(in EntityPath entityPath)
        {
            if (_world.Get(entityPath) is RenderItem2D renderItem)
            {
                return (int)renderItem.GetUnconstrainedBounds(_renderCtx).Width;
            }

            return 0;
        }

        public override int GetHeight(in EntityPath entityPath)
        {
            if (_world.Get(entityPath) is RenderItem2D renderItem)
            {
                return (int)renderItem.GetUnconstrainedBounds(_renderCtx).Height;
            }

            return 0;
        }

        private void Pause(
            WaitCondition condition,
            EntityQuery query,
            TimeSpan duration,
            TimeSpan delay)
        {
            if (delay == TimeSpan.Zero) { return; }
            if (!delay.Equals(duration))
            {
                Delay(delay);
            }
            else
            {
                _ctx.Wait(CurrentThread, condition, null, query);
            }
        }

        public override void Fade(
            EntityQuery query,
            TimeSpan duration,
            NsRational dstOpacity,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                ri.Fade(dstOpacity, duration, easeFunction);
            }

            Pause(WaitCondition.FadeCompleted, query, duration, delay);
        }

        public override void Move(
            EntityQuery query,
            TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                ri.Move(_renderCtx, dstX, dstY, duration, easeFunction);
            }

            Pause(WaitCondition.MoveCompleted, query, duration, delay);
        }

        public override void Zoom(
            EntityQuery query,
            TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            var dstScale = new Vector3(dstScaleX.Rebase(1.0f), dstScaleY.Rebase(1.0f), 1.0f);
            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                ri.Scale(dstScale, duration, easeFunction);
            }

            Pause(WaitCondition.ZoomCompleted, query, duration, delay);
        }

        public override void Rotate(
            EntityQuery query,
            TimeSpan duration,
            NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            var dstRot = new Vector3(dstRotationX.Value, dstRotationY.Value, dstRotationZ.Value);
            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                ri.Rotate(dstRot, duration, easeFunction);
            }

            Pause(WaitCondition.RotateCompleted, query, duration, delay);
        }

        public override void BezierMove(
            EntityQuery query,
            TimeSpan duration,
            CompositeBezier curve,
            NsEaseFunction easeFunction,
            bool wait)
        {
            if (duration <= TimeSpan.Zero) { return; }
            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                var segments = ImmutableArray.CreateBuilder<ProcessedBezierSegment>();
                foreach (CubicBezierSegment srcSeg in curve.Segments)
                {
                    Vector2 processPoint(BezierControlPoint srcPoint)
                        => ri.Point(_renderCtx, srcPoint.X, srcPoint.Y).XY();

                    segments.Add(new ProcessedBezierSegment(
                        processPoint(srcSeg.P0),
                        processPoint(srcSeg.P1),
                        processPoint(srcSeg.P2),
                        processPoint(srcSeg.P3)
                    ));
                }

                var processedCurve = new ProcessedBezierCurve(segments.ToImmutable());
                ri.BezierMove(processedCurve, duration, easeFunction);
            }

            if (wait)
            {
                _ctx.Wait(CurrentThread, WaitCondition.BezierMoveCompleted, null, query);
            }
        }

        public override void BeginTransition(
            EntityQuery query,
            TimeSpan duration,
            NsRational srcFadeAmount, NsRational dstFadeAmount,
            NsRational feather,
            NsEaseFunction easeFunction,
            string maskFileName,
            TimeSpan delay)
        {
            if (_ctx.Content.RequestTexture(maskFileName) is AssetRef<Texture> mask)
            {
                foreach (Sprite sprite in _world.Query<Sprite>(query))
                {
                    sprite.BeginTransition(
                        mask,
                        srcFadeAmount, dstFadeAmount,
                        duration,
                        easeFunction
                    );
                }
            }

            Pause(WaitCondition.TransitionCompleted, query, duration, delay);
        }
    }
}
