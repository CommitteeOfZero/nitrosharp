﻿using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Graphics.Effects;
using Veldrid;

namespace NitroSharp.Graphics.Objects
{
    internal sealed class Cube : Visual
    {
        private readonly AssetRef<BindableTexture> _front;
        private readonly AssetRef<BindableTexture> _back;
        private readonly AssetRef<BindableTexture> _left;
        private readonly AssetRef<BindableTexture> _right;
        private readonly AssetRef<BindableTexture> _top;
        private readonly AssetRef<BindableTexture> _bottom;

        private CubeEffect _cubeShader;
        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private BindableTexture _texture;

        public Cube(
            AssetRef<BindableTexture> front, AssetRef<BindableTexture> back,
            AssetRef<BindableTexture> left, AssetRef<BindableTexture> right,
            AssetRef<BindableTexture> top, AssetRef<BindableTexture> bottom)
        {
            _front = front;
            _back = back;
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
            Priority = 0;
        }

        public override RgbaFloat Color
        {
            get => _color;
            protected set
            {
                _color = value;
                if (_cubeShader != null)
                {
                    _cubeShader.Properties.Color = value;
                }
            }
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            var matrices = renderContext.SharedEffectProperties3D;
            _cubeShader = renderContext.Effects.Get<CubeEffect>(matrices);

            var gd = renderContext.Device;
            var factory = renderContext.Factory;

            _vb = gd.CreateStaticBuffer(s_vertices, BufferUsage.VertexBuffer);
            _ib = gd.CreateStaticBuffer(s_indices, BufferUsage.IndexBuffer);

            var frontTex = _front.Asset;
            uint width = frontTex.Width;
            uint height = frontTex.Height;

            Texture textureCube;
            textureCube = factory.CreateTexture(TextureDescription.Texture2D(
                width, height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled | TextureUsage.Cubemap));

            var cl = factory.CreateCommandList();
            cl.Begin();

            void copy(uint dstBaseArrayLayer, Texture source)
            {
                cl.CopyTexture(source, 0, 0, 0, 0, 0, textureCube,
                    0, 0, 0, 0, dstBaseArrayLayer, width, height, 1, 1);
            }

            copy(0, _right.Asset);
            copy(1, _left.Asset);
            copy(2, _top.Asset);
            copy(3, _bottom.Asset);
            copy(4, _front.Asset);
            copy(5, _back.Asset);

            cl.End();
            gd.SubmitCommands(cl);
            gd.DisposeWhenIdle(cl);
            gd.WaitForIdle();

            _texture = new BindableTexture(factory, textureCube);
            _cubeShader.Properties.Texture = _texture.GetTextureView();
            _cubeShader.Properties.Sampler = gd.Aniso4xSampler;

            _right.Dispose();
            _left.Dispose();
            _top.Dispose();
            _bottom.Dispose();
            _front.Dispose();
            _back.Dispose();
        }

        public override void Destroy(RenderContext renderContext)
        {
            _vb.Dispose();
            _ib.Dispose();
            _texture.Dispose();
        }

        public override void Render(RenderContext renderContext)
        {
            var cl = renderContext.CommandList;
            var properties = _cubeShader.Properties;
            properties.BeginRecording(cl);
            properties.World = Entity.Transform.GetTransformMatrix();
            properties.EndRecording();

            _cubeShader.Apply(cl);

            cl.ClearDepthStencil(1f);
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);

            cl.DrawIndexed(36);
        }

        private static readonly CubeVertex[] s_vertices = new CubeVertex[]
        {
            // Top
            new CubeVertex(new Vector3(-0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(0.5f,0.5f,0.5f)),
            new CubeVertex(new Vector3(-0.5f,0.5f,0.5f)),
            // Bottom
            new CubeVertex(new Vector3(-0.5f,-0.5f,0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,-0.5f)),
            new CubeVertex(new Vector3(-0.5f,-0.5f,-0.5f)),
            // Left
            new CubeVertex(new Vector3(-0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(-0.5f,0.5f,0.5f)),
            new CubeVertex(new Vector3(-0.5f,-0.5f,0.5f)),
            new CubeVertex(new Vector3(-0.5f,-0.5f,-0.5f)),
            // Right
            new CubeVertex(new Vector3(0.5f,0.5f,0.5f)),
            new CubeVertex(new Vector3(0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,-0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,0.5f)),
            // Back
            new CubeVertex(new Vector3(0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(-0.5f,0.5f,-0.5f)),
            new CubeVertex(new Vector3(-0.5f,-0.5f,-0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,-0.5f)),
            // Front
            new CubeVertex(new Vector3(-0.5f,0.5f,0.5f)),
            new CubeVertex(new Vector3(0.5f,0.5f,0.5f)),
            new CubeVertex(new Vector3(0.5f,-0.5f,0.5f)),
            new CubeVertex(new Vector3(-0.5f,-0.5f,0.5f)),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
    }
}
