using System;
using System.Numerics;
using Veldrid;

namespace NitroSharp.Graphics.Core;

internal sealed class ViewProjection : IDisposable
{
    public static ViewProjection CreateOrtho(GraphicsDevice graphicsDevice, in ScreenRectU viewport)
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            left: viewport.Left, right: viewport.Right,
            bottom: viewport.Bottom, top: viewport.Top,
            zNearPlane: 0.0f, zFarPlane: -1.0f
        );
        return new ViewProjection(graphicsDevice, projection);
    }

    public static ViewProjection CreatePerspective(GraphicsDevice graphicsDevice, float fov, float aspectRatio)
    {
        var view = Matrix4x4.CreateLookAt(
            cameraPosition: Vector3.Zero,
            cameraTarget: Vector3.UnitZ,
            cameraUpVector: Vector3.UnitY
        );
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fov,
            aspectRatio,
            nearPlaneDistance: 0.1f,
            farPlaneDistance: 1000.0f
        );
        return new ViewProjection(graphicsDevice, view * projection);
    }

    private ViewProjection(GraphicsDevice gd, in Matrix4x4 matrix)
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

    public void UpdateOrtho(CommandList cl, in ScreenRectU viewport)
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            left: viewport.Left, right: viewport.Right,
            bottom: viewport.Bottom, top: viewport.Top,
            zNearPlane: 0.0f, zFarPlane: -1.0f
        );
        cl.UpdateBuffer(Buffer.VdBuffer, 0, ref projection);
    }

    public void UpdateOrtho(GraphicsDevice graphicsDevice, in ScreenRectU viewport)
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            left: viewport.Left, right: viewport.Right,
            bottom: viewport.Bottom, top: viewport.Top,
            zNearPlane: 0.0f, zFarPlane: -1.0f
        );
        graphicsDevice.UpdateBuffer(Buffer.VdBuffer, 0, ref projection);
    }

    public void Dispose()
    {
        ResourceLayout.Dispose();
        Buffer.Dispose();
    }
}
