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

namespace NitroSharp
{
    internal partial class Builtins
    {
        public override void CreateCube(
            in EntityPath entityPath, int priority,
            string front, string back, string right,
            string left, string top, string bottom)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                string[] texturePaths = { right, left, top, bottom, front, back };
                World.Add(Cube.Load(resolvedPath, priority, _ctx.RenderContext, texturePaths));
            }
        }

        public override void CreateBacklog(in EntityPath path, int priority)
        {
            if (ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new BacklogView(resolvedPath, priority, _ctx));
            }
        }

        public override void ClearBacklog()
        {
            _ctx.Backlog.Clear();
        }

        public override void SetBacklog(string text)
        {
            TextSegment seg = TextBuffer.FromPXmlString(text, _ctx.ActiveProcess.FontConfig)
                .AssertSingleTextSegment()!;
            _ctx.Backlog.Append(seg);
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
            if (ResolvePath(path, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(knobImage) is SpriteTexture knob)
            {
                World.Add(new Scrollbar(
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
            if (Get(scrollbar) is Scrollbar sb &&
                Get(parent) is BacklogView backlog)
            {
                backlog.Scrollbar = sb.Id;
            }
        }

        public override float GetScrollbarValue(in EntityPath scrollbarEntity)
        {
            return Get(scrollbarEntity) is Scrollbar scrollbar
                ? scrollbar.GetValue()
                : 0;
        }

        public override void CreateChoice(in EntityPath entityPath)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new Choice(resolvedPath));
            }
        }

        public override void SetNextFocus(
            in EntityPath first,
            in EntityPath second,
            NsFocusDirection focusDirection)
        {
            static UiElement? getUiElement(Entity? entity) => entity switch
            {
                RenderItem2D { Parent: UiElement parent } => parent,
                UiElement element => element,
                _ => null
            };

            (Entity? entityA, Entity? entityB) = (Get(first), Get(second));
            if ((getUiElement(entityA), getUiElement(entityB))
                is (UiElement elementA, UiElement elementB))
            {
                elementA.SetNextFocus(focusDirection, elementB.Id);
            }
        }

        public override void SelectEnd()
        {
            _ctx.FocusedUiElement = EntityId.Invalid;
        }

        public override bool HandleInputEvents(in EntityPath uiElementPath)
        {
            if (Get(uiElementPath) is UiElement uiElement)
            {
                bool wasFocused = uiElement.IsFocused;
                bool selected = uiElement.HandleEvents(_ctx);
                if (uiElement.IsFocused)
                {
                    _ctx.FocusedUiElement = uiElement.Id;
                }
                else if (wasFocused)
                {
                    _ctx.FocusedUiElement = EntityId.Invalid;
                }

                if (_ctx.RequestedFocusChange is NsFocusDirection focusDirection)
                {
                    if (uiElement.IsFocused &&
                        Get(uiElement.GetNextFocus(focusDirection)) is UiElement nextFocus)
                    {
                        nextFocus.Focus(_renderCtx);
                        _ctx.FocusedUiElement = uiElement.Id;
                        _ctx.RequestedFocusChange = null;
                    }
                    else if (!World.Exists(_ctx.FocusedUiElement))
                    {
                        uiElement.Focus(_renderCtx);
                        _ctx.RequestedFocusChange = null;
                        _ctx.FocusedUiElement = uiElement.Id;
                    }
                }

                return selected;
            }

            return false;
        }

        public override void LoadColor(
            in EntityPath entityPath,
            uint width, uint height,
            NsColor color)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new ColorSource(
                    resolvedPath,
                    color.ToRgbaFloat(),
                    new Size(width, height)
                ));
            }
        }

        public override void LoadImage(in EntityPath entityPath, string source)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source) is SpriteTexture texture)
            {
                World.Add(new Image(resolvedPath, texture));
            }
        }

        public override void CreateRectangle(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint width, uint height,
            NsColor color)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new Sprite(
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
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source) is SpriteTexture texture)
            {
                World.Add(new Sprite(
                    resolvedPath,
                    priority,
                    texture
                ).WithPosition(_renderCtx, x, y));
            }
        }

        private SpriteTexture? ResolveSpriteSource(string src, in RectangleU? srcRect = null)
        {
            if (src is "SCREEN" or "Screen" or "VIDEO" or "Video")
            {
                Texture screenshotTexture = _renderCtx.CreateFullscreenTexture();
                var result = SpriteTexture.FromStandalone(screenshotTexture);
                _ctx.Defer(DeferredOperation.CaptureFramebuffer(screenshotTexture));
                return result;
            }

            Entity? srcEntity = Get(new EntityPath(src));
            if (srcEntity is ColorSource colorSrc)
            {
                return SpriteTexture.SolidColor(colorSrc.Color, colorSrc.Size);
            }
            if (srcEntity is Image img)
            {
                return img.Texture.WithSourceRectangle(srcRect);
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
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveSpriteSource(source, srcRect) is SpriteTexture texture)
            {
                World.Add(new Sprite(
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
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                var margin = new Vector4(0, 15, 34, 28);
                uint w = width is { Variant: NsTextDimensionVariant.Value, Value: { } sWidth }
                    ? (uint)sWidth : uint.MaxValue;
                uint h = height  is { Variant: NsTextDimensionVariant.Value, Value: { } sHeight }
                    ? (uint)sHeight : uint.MaxValue;

                World.Add(new TextBlock(
                    resolvedPath,
                    _renderCtx.Text,
                    priority,
                    pxmlText,
                    new Size(w, h),
                    _ctx.ActiveProcess.FontConfig,
                    margin
                ).WithPosition(_renderCtx, x, y));
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

            Vector4? outlinec = outlineOffset != NsOutlineOffset.Unspecified
                ? outlineColor.ToVector4()
                : null;

            _ctx.ActiveProcess.FontConfig
                .WithDefaultSize(new PtFontSize(mapFontSize(size)))
                .WithOutlineColor(outlinec)
                .WithDefaultColor(color.ToVector4());
        }

        public override void CreateDialogueBox(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint width, uint height,
            bool inheritTransform)
        {
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                World.Add(new DialogueBox(
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
            if (ResolvePath(path, out ResolvedEntityPath resolvedPath)
                && resolvedPath.Parent is RenderItem2D box)
            {
                var margin = new Vector4(0, 10, 0, 0);
                NsScriptThread thread = _ctx.VM.ActivateDialogueBlock(blockToken);
                var page = World.Add(new DialoguePage(
                    resolvedPath,
                    box.Key.Priority,
                    new Size(maxWidth, maxHeight),
                    lineSpacing,
                    margin,
                    thread
                )).WithPosition(_renderCtx, default, default);
                World.SetAlias(page.Id, new EntityPath(page.Id.Name.ToString()));
            }
        }

        public override void ClearDialoguePage(in EntityPath dialoguePage)
        {
            if (Get(dialoguePage) is DialoguePage page)
            {
                page.Clear();
            }
        }

        public override void AppendDialogue(in EntityPath dialoguePage, string text)
        {
            if (Get(dialoguePage) is DialoguePage page)
            {
                page.Append(_ctx, text, _ctx.ActiveProcess.FontConfig);
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

        public override void WaitText(EntityQuery query, TimeSpan timeout)
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
            if (ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.RequestTexture(imagePath) is AssetRef<Texture> texture)
            {
                World.Add(new AlphaMask(
                    resolvedPath,
                    priority,
                    texture,
                    inheritTransform
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override Vector2 GetPosition(in EntityPath entityPath)
        {
            return Get(entityPath) is RenderItem2D renderItem
                ? renderItem.Transform.Position.XY()
                : Vector2.Zero;
        }

        public override int GetWidth(in EntityPath entityPath)
        {
            return Get(entityPath) is RenderItem2D renderItem
                ? (int)renderItem.GetUnconstrainedBounds(_renderCtx).Width
                : 0;
        }

        public override int GetHeight(in EntityPath entityPath)
        {
            return Get(entityPath) is RenderItem2D renderItem
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
                Delay(AdjustDuration(delay));
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
            duration = AdjustDuration(duration);
            delay = AdjustDuration(delay);
            foreach (RenderItem2D ri in Query<RenderItem2D>(query))
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
            duration = AdjustDuration(duration);
            delay = AdjustDuration(delay);
            foreach (RenderItem2D ri in Query<RenderItem2D>(query))
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
            duration = AdjustDuration(duration);
            delay = AdjustDuration(delay);
            var dstScale = new Vector3(dstScaleX.Rebase(1.0f), dstScaleY.Rebase(1.0f), 1.0f);
            foreach (RenderItem2D ri in Query<RenderItem2D>(query))
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
            duration = AdjustDuration(duration);
            delay = AdjustDuration(delay);
            foreach (RenderItem ri in Query<RenderItem>(query))
            {
                ri.Rotate(dstRotationX, dstRotationY, dstRotationZ, duration, easeFunction);
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
            duration = AdjustDuration(duration);
            foreach (RenderItem2D ri in Query<RenderItem2D>(query))
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
            duration = AdjustDuration(duration);
            delay = AdjustDuration(delay);
            if (_ctx.Content.RequestTexture(maskFileName) is AssetRef<Texture> mask)
            {
                foreach (Sprite sprite in Query<Sprite>(query))
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

        private TimeSpan AdjustDuration(TimeSpan duration)
        {
            return _ctx.Skipping
                ? TimeSpan.FromSeconds(duration.TotalSeconds / 10.0d)
                : duration;
        }
    }
}
