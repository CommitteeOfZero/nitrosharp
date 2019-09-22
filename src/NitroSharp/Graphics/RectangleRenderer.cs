using NitroSharp.Experimental;

namespace NitroSharp.Graphics
{
    internal sealed class RectangleRenderer
    {
        private readonly World _world;
        private readonly QuadBatcher _quadBatcher;
        private readonly EntityHub<RectangleStorage> _rectangles;

        public RectangleRenderer(World world, RenderContext renderContext)
        {
            _world = world;
            _quadBatcher = renderContext.QuadBatcher;
            _rectangles = world.Rectangles;
        }

        public void ProcessRectangles()
        {
            TransformProcessor.ProcessTransforms(_world, _rectangles.Active);
            RectangleStorage sprites = _rectangles.Active;
            _quadBatcher.BatchQuads(
                sprites.CommonProperties.All,
                sprites.LocalBounds.All,
                _quadBatcher.SolidColorMaterial,
                sprites.Transforms.All
            );
        }
    }
}
