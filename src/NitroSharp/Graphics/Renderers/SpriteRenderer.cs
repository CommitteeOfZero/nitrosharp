using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Components;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal class SpriteRenderer : GameSystem
    {
        private readonly World _world;
        private readonly QuadBatcher _quadBatcher;
        private readonly ContentManager _content;
        private AssetRef<BindableTexture>[] _textures;

        public SpriteRenderer(World world, RenderContext renderContext, ContentManager contentManager)
        {
            world.SpriteAdded += OnSpriteAdded;
            world.SpriteRemoved += OnSpriteRemoved;
            _world = world;
            _quadBatcher = renderContext.QuadBatcher;
            _content = contentManager;
            _textures = new AssetRef<BindableTexture>[World.InitialSpriteCount];
        }

        private void OnSpriteAdded(Entity entity)
        {
            ushort index = entity.Index;
            ArrayUtil.EnsureCapacity(ref _textures, index + 1);

            SpriteComponent sprite = _world.Sprites.SpriteComponents.GetValue(entity);
            if (sprite.Image != null)
            {
                _textures[index] = _content.Get<BindableTexture>(sprite.Image);
            }
        }

        private void OnSpriteRemoved(Entity entity)
        {
            ushort index = entity.Index;
            ref AssetRef<BindableTexture> assetRef = ref _textures[index];
            assetRef?.Dispose();
            assetRef = null;
        }

        public void ProcessSprites(Sprites spriteTable)
        {
            TransformProcessor.ProcessTransforms(_world, spriteTable);
            ReadOnlySpan<Matrix4x4> transforms = spriteTable.TransformMatrices.Enumerate();

            Span<SpriteComponent> sprites = spriteTable.SpriteComponents.MutateAll();
            Span<RgbaFloat> colors = spriteTable.Colors.MutateAll();
            Span<int> priorities = spriteTable.RenderPriorities.MutateAll();

            QuadBatcher quadBatcher = _quadBatcher;
            for (int i = 0; i < sprites.Length; i++)
            {
                SpriteComponent sprite = sprites[i];
                int renderPriority = priorities[i];
                if (renderPriority > 0 && colors[i].A > 0)
                {
                    ref readonly Matrix4x4 transform = ref transforms[i];
                    AssetRef<BindableTexture> textureRef = _textures[i];
                    TextureView texView = textureRef.Asset.GetTextureView();

                    ref RectangleF srcRect = ref sprite.SourceRectangle;
                    var dstRect = new RectangleF(0, 0, srcRect.Width, srcRect.Height);

                    quadBatcher.SetTransform(transform);
                    quadBatcher.DrawImage(texView, srcRect, dstRect, ref colors[i], renderPriority);
                }
            }
        }
    }
}
