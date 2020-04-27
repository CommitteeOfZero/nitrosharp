using System;
using System.Numerics;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics.Core
{
    internal sealed class ViewProjection : IDisposable
    {
        public static ViewProjection CreateOrtho(GraphicsDevice graphicsDevice, Size viewportSize)
        {
            var projection = Matrix4x4.CreateOrthographicOffCenter(
                left: 0, right: viewportSize.Width,
                bottom: viewportSize.Height, top: 0,
                zNearPlane: 0.0f, zFarPlane: -1.0f
            );
            return new ViewProjection(graphicsDevice, projection);
        }

        public ViewProjection(GraphicsDevice gd, in Matrix4x4 matrix)
        {
            ResourceFactory rf = gd.ResourceFactory;
            ResourceLayout = rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                )
            ));
            Buffer = GpuBuffer<Matrix4x4>.CreateUniform(gd, matrix);
        }

        public ResourceLayout ResourceLayout { get; }
        public GpuBuffer<Matrix4x4> Buffer { get; }

        public void Dispose()
        {
            ResourceLayout.Dispose();
            Buffer.Dispose();
        }
    }
}
