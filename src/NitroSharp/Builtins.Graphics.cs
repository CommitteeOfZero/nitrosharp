using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Text;
using Veldrid;

namespace NitroSharp
{
    internal partial class Builtins
    {
        public override void CreateRectangle(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint width, uint height,
            NsColor color)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                ColorRect rect = _world.Add(new ColorRect(
                    resolvedPath,
                    priority,
                    new Size(width, height),
                    color.ToRgbaFloat()
                ).WithPosition(_world, _renderCtx, x, y));

                if (_world.Get(rect.Parent) is AlphaMask alphaMask)
                {
                    rect.AlphaMaskOpt = alphaMask.Texture;
                }
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

        public override void CreateSprite(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            string source)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveImageSource(source) is AssetRef<Texture> texture)
            {
                Sprite sprite = _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    texture
                ).WithPosition(_world, _renderCtx, x, y));

                if (_world.Get(sprite.Parent) is AlphaMask alphaMask)
                {
                    sprite.AlphaMaskOpt = alphaMask.Texture;
                }
            }
        }

        private AssetRef<Texture>? ResolveImageSource(string src)
        {
            return _world.Get(new EntityPath(src)) is Image img
                ? img.Texture.Clone()
                : _ctx.Content.RequestTexture(src);
        }

        public override void CreateSpriteEx(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint srcX, uint srcY,
            uint width, uint height,
            string source)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && ResolveImageSource(source) is AssetRef<Texture> texture)
            {
                _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    texture,
                    new RectangleU(srcX, srcY, width, height)
                ).WithPosition(_world, _renderCtx, x, y));
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
                var textBuffer = TextBuffer.FromPXmlString(pxmlText, _ctx.FontConfig);
                Size parentBounds = _world.Get(resolvedPath.ParentId) switch
                {
                    RenderItem2D parent => parent.GetUnconstrainedBounds(_renderCtx),
                    _ => _renderCtx.DesignResolution
                };
                uint w = width switch
                {
                    { Variant: NsTextDimensionVariant.Value, Value: {} val } => (uint)val,
                    //{ Variant: NsTextDimensionVariant.Inherit } => parentBounds.Width,
                    { Variant: NsTextDimensionVariant.Auto } => 0
                };
                uint h = width switch
                {
                    { Variant: NsTextDimensionVariant.Value, Value: {} val } => (uint)val,
                    //{ Variant: NsTextDimensionVariant.Inherit } => parentBounds.Height,
                    { Variant: NsTextDimensionVariant.Auto } => 0
                };
                Size? bounds = (w, h) switch
                {
                    (0, 0) => null,
                    _ => new Size(w, h)
                };

                if (textBuffer.AssertSingleTextSegment() is TextSegment textSegment)
                {
                    var layout = new TextLayout(_ctx.GlyphRasterizer, textSegment.TextRuns.AsSpan(), bounds);
                    _world.Add(new TextRect(
                        resolvedPath,
                        _renderCtx.Text,
                        priority,
                        layout
                    ).WithPosition(_world, _renderCtx, x, y));
                }
            }
        }

        public override void SetFont(
            string family, int size,
            NsColor color, NsColor outlineColor,
            NsFontWeight weight,
            NsOutlineOffset outlineOffset)
        {
            _ctx.FontConfig
                .WithDefaultSize(new PtFontSize((int)(size * 0.5)))
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
                ).WithPosition(_world, _renderCtx, x, y));
            }
        }

        public override void CreateAlphaMask(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            string maskPath,
            bool inheritTransform)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.RequestTexture(maskPath) is AssetRef<Texture> texture)
            {
                _world.Add(new AlphaMask(
                    resolvedPath,
                    priority,
                    texture,
                    inheritTransform
                ).WithPosition(_world, _renderCtx, x, y));
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
                ri.Color.SetAlpha(0.3f);
            }
            Delay(delay);
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
                Vector3 destination = ri.Point(_world, _renderCtx, dstX, dstY);
                ri.Move(_world, _renderCtx, dstX, dstY, duration, easeFunction);
                //if (ri.AnimPropagateFlags.HasFlag(AnimPropagateFlags.Move))
                //{
                //    foreach (RenderItem2D child in _world.Children<RenderItem2D>(ri))
                //    {
                //        child.Move(destination, duration, easeFunction);
                //    }
                //}
            }

            Delay(delay);
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
                if (ri.AnimPropagateFlags.HasFlag(AnimPropagateFlags.Scale))
                {
                    foreach (RenderItem2D child in _world.Children<RenderItem2D>(ri))
                    {
                        child.Scale(dstScale, duration, easeFunction);
                    }
                }
            }

            Delay(delay);
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
                if (ri.AnimPropagateFlags.HasFlag(AnimPropagateFlags.Rotate))
                {
                    foreach (RenderItem child in _world.Children<RenderItem>(ri))
                    {
                        child.Rotate(dstRot, duration, easeFunction);
                    }
                }
            }

            Delay(delay);
        }
    }
}
