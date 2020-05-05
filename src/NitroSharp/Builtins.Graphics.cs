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
            NsCoordinate x,
            NsCoordinate y,
            uint width,
            uint height,
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
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                if (_ctx.Content.RequestTexture(source) is AssetRef<Texture> textureRef)
                {
                    Sprite sprite = _world.Add(new Sprite(
                        resolvedPath,
                        priority,
                        textureRef
                    ).WithPosition(_world, _renderCtx, x, y));

                    if (_world.Get(sprite.Parent) is AlphaMask alphaMask)
                    {
                        sprite.AlphaMaskOpt = alphaMask.Texture;
                    }
                }
            }
        }

        public override void CreateSpriteEx(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            uint srcX, uint srcY,
            uint width, uint height,
            in EntityPath srcEntityPath)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _world.Get(srcEntityPath) is Image srcImage)
            {
                _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    srcImage.Texture.Clone(),
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
                var textBuffer = TextBuffer.FromPXmlString(pxmlText, _ctx.FontConfig, new PtFontSize(20));
                if (textBuffer.AssertSingleTextSegment() is TextSegment textSegment)
                {
                    var layout = new TextLayout(_ctx.GlyphRasterizer, textSegment.TextRuns.AsSpan(), null);
                    _world.Add(new TextRect(
                        resolvedPath,
                        _renderCtx.Text,
                        priority,
                        layout
                    ).WithPosition(_world, _renderCtx, x, y));
                }
            }
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
        }

        public override void Move(
            EntityQuery query,
            TimeSpan duration,
            NsCoordinate dstX, NsCoordinate dstY,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            void moveCore(RenderItem2D renderItem)
            {
                Vector3 destination = renderItem.Point(_world, _renderCtx, dstX, dstY);
                if (duration > TimeSpan.Zero)
                {
                    var anim = new MoveAnimation(
                        renderItem,
                        startPosition: renderItem.Transform.Position,
                        destination,
                        duration,
                        easeFunction
                    );
                    _world.ActivateAnimation(anim);
                }
                else
                {
                    renderItem.Transform.Position = destination;
                }
            }

            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                moveCore(ri);
            }
        }

        public override void Zoom(
            EntityQuery query,
            TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            void zoomCore(RenderItem2D renderItem)
            {
                var dstScale = new Vector3(dstScaleX.Rebase(1.0f), dstScaleY.Rebase(1.0f), 1.0f);
                if (duration > TimeSpan.Zero)
                {
                    var anim = new ScaleAnimation(
                        renderItem,
                        startScale: renderItem.Transform.Scale,
                        dstScale,
                        duration,
                        easeFunction
                    );
                    _world.ActivateAnimation(anim);
                }
                else
                {
                    renderItem.Transform.Scale = dstScale;
                }
            }

            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                zoomCore(ri);
                if (ri is ConstraintBox { InheritTransform: false })
                {
                    continue;
                }
                foreach (RenderItem2D child in _world.Children<RenderItem2D>(ri))
                {
                    zoomCore(child);
                }
            }
        }

        public override void Rotate(
            EntityQuery query,
            TimeSpan duration,
            NsNumeric dstRotationX, NsNumeric dstRotationY, NsNumeric dstRotationZ,
            NsEaseFunction easeFunction,
            TimeSpan delay)
        {
            void rotateCore(RenderItem renderItem)
            {
                var dstRot = new Vector3(
                    dstRotationX.Value,
                    dstRotationY.Value,
                    dstRotationZ.Value
                );
                if (duration > TimeSpan.Zero)
                {
                    var anim = new RotateAnimation(
                        renderItem,
                        startRot: renderItem.Transform.Rotation,
                        dstRot,
                        duration,
                        easeFunction
                    );
                    _world.ActivateAnimation(anim);
                }
                else
                {
                    renderItem.Transform.Rotation = dstRot;
                }
            }

            foreach (RenderItem2D ri in _world.Query<RenderItem2D>(query))
            {
                rotateCore(ri);
            }
        }
    }
}
