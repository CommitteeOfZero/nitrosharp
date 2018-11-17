using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct SpriteSystemData
    {
        public AssetRef<BindableTexture> AssetRef;
        public TextureView TextureView;
    }

    internal class SpriteRenderer
    {
        private readonly World _world;
        private readonly SpriteTable _sprites;
        private readonly QuadBatcher _quadBatcher;
        private readonly ContentManager _content;

        public SpriteRenderer(World world, RenderContext renderContext, ContentManager content)
        {
            _world = world;
            _sprites = world.Sprites;
            _quadBatcher = renderContext.QuadBatcher;
            _content = content;
        }

        public void ProcessSprites()
        {
            SpriteTable sprites = _sprites;

            var added = sprites.AddedEntities;
            foreach (Entity e in added)
            {
                ImageSource source = sprites.ImageSources.GetValue(e);
                var assetRef = _content.Get<BindableTexture>(source.Image);
                ref SpriteSystemData data = ref sprites.SystemData.Mutate(e);
                data.AssetRef = assetRef;
                data.TextureView = assetRef.Asset.GetTextureView();

            }

            var toRecycle = sprites.SystemData.RecycledComponents;
            foreach (SpriteSystemData data in toRecycle)
            {
                data.AssetRef.Dispose();
            }

            

            ReadOnlySpan<ImageSource> sources = sprites.ImageSources.Enumerate();
            ReadOnlySpan<Matrix4x4> transforms = sprites.TransformMatrices.Enumerate();
            ReadOnlySpan<RgbaFloat> colors = sprites.Colors.Enumerate();
            ReadOnlySpan<RenderItemKey> priorities = sprites.SortKeys.Enumerate();

            ReadOnlySpan<SpriteSystemData> systemData = sprites.SystemData.Enumerate();

            QuadBatcher quadBatcher = _quadBatcher;
            for (int i = 0; i < sources.Length; i++)
            {
                ImageSource source = sources[i];
                RenderItemKey renderPriority = priorities[i];
                if (renderPriority.Priority > 0 && colors[i].A > 0)
                {
                    ref readonly Matrix4x4 transform = ref transforms[i];
                    TextureView texView = systemData[i].TextureView;

                    ref readonly RectangleF srcRect = ref source.SourceRectangle;
                    var dstRect = new RectangleF(0, 0, srcRect.Width, srcRect.Height);

                    quadBatcher.SetTransform(transform);
                    RgbaFloat c = colors[i];
                    quadBatcher.DrawImage(texView, srcRect, dstRect, ref c, renderPriority);
                }
            }
        }
    }
}
