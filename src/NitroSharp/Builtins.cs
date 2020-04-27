using System;
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
    internal sealed class Builtins : BuiltInFunctions
    {
        private readonly World _world;
        private readonly Context _ctx;
        private readonly RenderContext _renderCtx;

        public Builtins(Context context)
        {
            _ctx = context;
            _renderCtx = context.RenderContext;
            _world = context.World;
        }

        public override void LoadImage(in EntityPath entityPath, string fileName)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _ctx.Content.RequestTexture(fileName) is AssetRef<Texture> texture)
            {
                _world.Add(new Image(resolvedPath, texture));
            }
        }

        public override void CreateDialogueBox(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height,
            bool inheritTransform)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new AlphaMask(
                    resolvedPath,
                    priority,
                    null,
                    new SizeF(width, height),
                    inheritTransform
                ).WithPosition(_renderCtx, x, y));
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
                    null,
                    inheritTransform
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override void CreateChoice(in EntityPath entityPath)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                _world.Add(new Choice(resolvedPath));
            }
        }

        public override void DestroyEntities(EntityQuery query)
        {
            foreach (Entity entity in _world.Query(query).AsSpan())
            {
                _world.DestroyEntity(entity);
            }
        }

        public override void CreateEntity(in EntityPath path)
        {
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
                    ).WithPosition(_renderCtx, x, y));

                    if (_world.Get(sprite.Parent) is AlphaMask alphaMask)
                    {
                        sprite.AlphaMaskOpt = alphaMask.Texture;
                        sprite.Transform.Inherit = alphaMask.InheritTransform;
                    }
                }
            }
        }

        public override void CreateSpriteEx(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            int srcX, int srcY,
            int width, int height,
            in EntityPath srcEntityPath)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath)
                && _world.Get(srcEntityPath) is Image srcImage)
            {
                _world.Add(new Sprite(
                    resolvedPath,
                    priority,
                    srcImage.Texture.Clone(),
                    new RectangleF(srcX, srcY, width, height)
                ).WithPosition(_renderCtx, x, y));
            }
        }

        public override void CreateRectangle(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height,
            NsColor color)
        {
            if (_world.ResolvePath(entityPath, out ResolvedEntityPath resolvedPath))
            {
                ColorRect rect = _world.Add(new ColorRect(
                    resolvedPath,
                    priority,
                    new SizeF(width, height),
                    color.ToRgbaFloat()
                ).WithPosition(_renderCtx, x, y));

                if (_world.Get(rect.Parent) is AlphaMask alphaMask)
                {
                    rect.AlphaMaskOpt = alphaMask.Texture;
                    rect.Transform.Inherit = alphaMask.InheritTransform;
                }
            }
        }

        public override void Zoom(
            EntityQuery query,
            TimeSpan duration,
            NsRational dstScaleX, NsRational dstScaleY,
            NsEasingFunction easingFunction,
            TimeSpan delay)
        {
            foreach (Entity e in _world.Query(query).AsSpan())
            {
                if (e is RenderItem2D ri)
                {
                    ri.Transform.Scale = new Vector3(2.0f);
                }
            }
        }

        public override void CreateTextBlock(
            in EntityPath entityPath,
            int priority,
            NsCoordinate x, NsCoordinate y,
            NsDimension width, NsDimension height,
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
                        priority,
                        _renderCtx.Text,
                        layout
                    ).WithPosition(_renderCtx, x, y));
                }
            }
        }

        public override void WaitForInput()
        {
            VM.SuspendThread(VM.CurrentThread!, TimeSpan.FromSeconds(4));
        }
    }
}
