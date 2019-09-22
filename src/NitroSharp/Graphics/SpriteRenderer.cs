using System.Diagnostics;
using NitroSharp.Content;
using NitroSharp.Experimental;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal sealed class SpriteRenderer
    {
        private readonly EntityHub<SpriteStorage> _sprites;
        private readonly QuadBatcher _quadBatcher;
        private readonly ContentManager _content;

        public SpriteRenderer(World world, RenderContext renderContext, ContentManager content)
        {
            _sprites = world.Sprites;
            _quadBatcher = renderContext.QuadBatcher;
            _content = content;
        }

        public void ProcessSprites()
        {
            SpriteStorage sprites = _sprites.Active;
            _quadBatcher.BatchQuads(
                sprites.CommonProperties.All,
                sprites.LocalBounds.All,
                sprites.Materials.All,
                sprites.Transforms.All
            );
        }

        public void ProcessNewSprites()
        {
            SpriteStorage sprites = _sprites.Uninitialized;
            for (uint i = 0; i < sprites.Count; i++)
            {
                ImageSource source = sprites.ImageSources[i];
                Texture? tex = _content.TryGetTexture(source.ImageId);
                Debug.Assert(tex != null);
                sprites.Materials[i] = _quadBatcher
                    .CreateMaterial(tex, source.SourceRectangle);
            }
        }
    }
}
