using System;
using System.Collections.Immutable;
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
        public override void CreateBacklog(in EntityPath path, int priority)
        {
            if (_world.ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new Backlog(resolvedPath, priority, _world.History));
            }
        }

        public override void ClearBacklog()
        {

        }

        public override void SetBacklog(string text)
        {
            _world.History.Add(text);
        }

        public override Vector2 GetCursorPosition()
        {
            return _ctx.InputContext.MousePosition;
        }

        public override void CreateScrollbar(
            in EntityPath path,
            int priority,
            int x1, int y1,
            int x2, int y2,
            NsRational initialValue,
            NsScrollDirection scrollDirection,
            string knobImage)
        {
            if (_world.ResolvePath(path, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(knobImage) is SpriteTexture knob)
            {
                _world.Add(new Scrollbar(
                    resolvedPath,
                    priority,
                    scrollDirection,
                    knob,
                    new Vector2(x1, y1),
                    new Vector2(x2, y2),
                    initialValue.Rebase(1.0f)
                ));
            }
        }

        public override void SetScrollbar(in EntityPath scrollbar, in EntityPath parent)
        {
            if (_world.Get(parent) is Backlog backlog)
            {
                // TODO
            }
        }

        public override float GetScrollbarValue(in EntityPath scrollbarEntity)
        {
            return _world.Get(scrollbarEntity) is Scrollbar scrollbar
                ? scrollbar.GetValue()
                : 0;
        }

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

        public override bool HandleInputEvents(in EntityPath uiElementPath)
        {
            if (_world.Get(uiElementPath) is UiElement uiElement)
            {
                return uiElement.HandleEvents();
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

        public override void LoadImage(in EntityPath entityPath, string source)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source) is SpriteTexture texture)
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
            }
        }

        private SpriteTexture? ResolveSpriteSource(string src, in RectangleU? srcRect = null)
        {
            if (src == "SCREEN")
            {
                var result = SpriteTexture.FromStandalone(_renderCtx.CreateFullscreenTexture());
                _ctx.Wait(
                    CurrentThread,
                    WaitCondition.FrameReady,
                    timeout: null,
                    entityQuery: null,
                    result.Standalone
                );
                return result;
            }

            Entity? srcEntity = _world.Get(new EntityPath(src));
            if (srcEntity is ColorSource colorSrc)
            {
                return SpriteTexture.SolidColor(colorSrc.Color, colorSrc.Size);
            }
            if (srcEntity is Image img)
            {
                return img.Texture;
            }

            if (_ctx.Content.RequestTexture(src) is AssetRef<Texture> asset)
            {
                return SpriteTexture.FromAsset(asset, srcRect);
            }

            return null;
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
                var margin = new Vector4(0, 15, 34, 28);
                var textBuffer = TextBuffer.FromPXmlString(pxmlText, _ctx.FontConfig);

                uint w = width is { Variant: NsTextDimensionVariant.Value, Value: { } sWidth }
                    ? (uint)sWidth : uint.MaxValue;
                uint h = height  is { Variant: NsTextDimensionVariant.Value, Value: { } sHeight }
                    ? (uint)sHeight : uint.MaxValue;

                if (textBuffer.AssertSingleTextSegment() is TextSegment textSegment)
                {
                    var layout = new TextLayout(
                        _ctx.GlyphRasterizer,
                        textSegment.TextRuns.AsSpan(),
                        new Size(w, h)
                    );
                    _world.Add(new TextBlock(
                        resolvedPath,
                        _renderCtx.Text,
                        priority,
                        layout,
                        margin
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
                //23 => 22,
                //26 => 25,
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

        public override void LoadDialogueBlock(
            in DialogueBlockToken blockToken,
            uint maxWidth, uint maxHeight,
            int letterSpacing, int lineSpacing)
        {
            var path = new EntityPath($"{blockToken.BoxName}/{blockToken.BlockName}");
            if (_world.ResolvePath(path, out ResolvedEntityPath resolvedPath)
                && resolvedPath.Parent is RenderItem2D box)
            {
                var margin = new Vector4(0, 10, 0, 0);
                ThreadContext thread = _ctx.VM.ActivateDialogueBlock(blockToken);
                var page = _world.Add(new DialoguePage(
                    resolvedPath,
                    box.Key.Priority,
                    new Size(maxWidth, maxHeight),
                    lineSpacing,
                    margin,
                    thread
                )).WithPosition(_renderCtx, default, default);
                _world.SetAlias(page.Id, new EntityPath(page.Id.Name.ToString()));
            }
        }

        public override void ClearDialoguePage(in EntityPath dialoguePage)
        {
            if (_world.Get(dialoguePage) is DialoguePage page)
            {
                page.Clear();
            }
        }

        public override void AppendDialogue(in EntityPath dialoguePage, string text)
        {
            if (_world.Get(dialoguePage) is DialoguePage page)
            {
                var buffer = TextBuffer.FromPXmlString(text, _ctx.FontConfig);
                page.Append(_renderCtx, buffer);
            }
        }

        public override void LineEnd(in EntityPath dialoguePage)
        {
            _ctx.Wait(
                CurrentThread,
                WaitCondition.LineRead,
                null,
                new EntityQuery(dialoguePage.Value)
            );
        }

        public override void WaitText(in EntityQuery query, TimeSpan timeout)
        {
            _ctx.Wait(CurrentThread, WaitCondition.EntityIdle, null, query);
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

        public override Vector2 GetPosition(in EntityPath entityPath)
        {
            return _world.Get(entityPath) is RenderItem2D renderItem
                ? renderItem.Transform.Position.XY()
                : Vector2.Zero;
        }

        public override int GetWidth(in EntityPath entityPath)
        {
            return _world.Get(entityPath) is RenderItem2D renderItem
                ? (int)renderItem.GetUnconstrainedBounds(_renderCtx).Width
                : 0;
        }

        public override int GetHeight(in EntityPath entityPath)
        {
            return _world.Get(entityPath) is RenderItem2D renderItem
                ? (int)renderItem.GetUnconstrainedBounds(_renderCtx).Height
                : 0;
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
