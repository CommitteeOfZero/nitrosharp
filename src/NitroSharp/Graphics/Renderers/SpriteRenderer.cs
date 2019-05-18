using System.Collections.Generic;
using NitroSharp.Content;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class SpriteRenderer
    {
        internal struct SystemData
        {
            public Texture Texture;
            public AssetId TextureId;
        }

        private readonly SpriteTable _sprites;
        private readonly QuadBatcher _quadBatcher;
        private readonly ContentManager _content;

        private SystemData[] _systemData = new SystemData[World.InitialSpriteCount];
        private readonly List<SystemData> _recycledData = new List<SystemData>();

        public SpriteRenderer(World world, RenderContext renderContext, ContentManager content)
        {
            _sprites = world.Sprites;
            _quadBatcher = renderContext.QuadBatcher;
            _content = content;
        }

        public void ProcessSprites()
        {
            SpriteTable sprites = _sprites;
            sprites.RearrangeSystemComponents(ref _systemData, _recycledData);
            foreach (Entity e in sprites.NewEntities)
            {
                ushort index = sprites.LookupIndex(e);
                ImageSource source = sprites.ImageSources.GetValue(index);
                Texture tex = _content.GetTexture(source.Image);
                ref SystemData data = ref _systemData[index];
                data.TextureId = source.Image;
                data.Texture = tex;
            }

            foreach (SystemData data in _recycledData)
            {
                if (data.Texture != null)
                {
                    _content.UnrefAsset(data.TextureId);
                }
            }

            QuadBatcher quadBatcher = _quadBatcher;
            SystemData[] systemData = _systemData;
            foreach (Sprite sprite in sprites)
            {
                if (sprite.SortKey.Priority > 0 && sprite.Color.A > 0.0f)
                {
                    Texture texture = systemData[sprite.Index].Texture;
                    ref readonly RectangleF srcRect = ref sprite.ImageSource.SourceRectangle;
                    var dstRect = new RectangleF(0, 0, srcRect.Width, srcRect.Height);

                    quadBatcher.SetTransform(sprite.Transform);
                    RgbaFloat c = sprite.Color;
                    quadBatcher.DrawImage(texture, srcRect, dstRect, ref c, sprite.SortKey, sprite.BlendMode);
                }
            }
        }
    }
}
