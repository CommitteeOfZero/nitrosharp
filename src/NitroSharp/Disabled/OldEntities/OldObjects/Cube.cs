using System;
using System.Numerics;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
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

        private DeviceBuffer _viewProjectionBuffer;
        private VertexBuffer<CubeVertex> _vb;
        private DeviceBuffer _ib;
        private BindableTexture _texture;
        private ResourceSet _viewProjectionSet;
        private ResourceSet _cubeResourceSet;
        private Pipeline _pipeline;
        private CircularVertexBuffer<CubeInstanceData> _instanceData;

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
        }

        public override void CreateDeviceObjects(RenderContext renderContext)
        {
            GraphicsDevice gd = renderContext.Device;
            ResourceFactory factory = renderContext.ResourceFactory;
            Size designResolution = renderContext.DesignResolution;

            var view = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathUtil.PI / 3.0f,
                (float)designResolution.Width / designResolution.Height,
                0.1f,
                1000.0f);

            Matrix4x4 viewProjection = view * projection;
            _viewProjectionBuffer = gd.CreateStaticBuffer(ref viewProjection, BufferUsage.UniformBuffer);

            (Shader vs, Shader fs) = renderContext.ShaderLibrary.GetShaderSet("Cube");
            var shaderSetDesc = new ShaderSetDescription(
                new[] { CubeVertex.LayoutDescription, CubeInstanceData.LayoutDescription },
                new[] { vs, fs });

            CommandList cl = factory.CreateCommandList();
            cl.Begin();

            _vb = new VertexBuffer<CubeVertex>(gd, cl, s_vertices);
            _ib = gd.CreateStaticBuffer(s_indices, BufferUsage.IndexBuffer);

            BindableTexture frontTex = _front.Asset;
            uint width = frontTex.Width;
            uint height = frontTex.Height;

            Texture textureCube = factory.CreateTexture(TextureDescription.Texture2D(
                width, height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled | TextureUsage.Cubemap));

            void copy(uint dstBaseArrayLayer, Texture source)
            {
                cl.CopyTexture(
                    source, 0, 0, 0, 0, 0, textureCube,
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

            _texture = new BindableTexture(factory, textureCube);

            _viewProjectionSet = factory.CreateResourceSet(new ResourceSetDescription(
                renderContext.ViewProjection.ResourceLayout, _viewProjectionBuffer));

            ResourceLayout cubeResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            _cubeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                cubeResourceLayout,
                _texture.GetTextureView(),
                gd.Aniso4xSampler));

            _pipeline = factory.CreateGraphicsPipeline(
                new GraphicsPipelineDescription(
                    BlendStateDescription.SingleOverrideBlend,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleList,
                    shaderSetDesc,
                    new[] { renderContext.ViewProjection.ResourceLayout, cubeResourceLayout },
                    renderContext.MainSwapchain.Framebuffer.OutputDescription));

            _instanceData = new CircularVertexBuffer<CubeInstanceData>(gd, 1);
        }

        public override void DestroyDeviceObjects(RenderContext renderContext)
        {
            _vb.Dispose();
            _ib.Dispose();
            _texture.Dispose();
        }

        public override void Render(RenderContext renderContext)
        {
            Matrix4x4 world = Entity.Transform.GetTransformMatrix();

            _instanceData.Begin();

            RenderBucket bucket = renderContext.MainBucket;
            var instance = new CubeInstanceData(ref _color, world);
            var submission = new RenderBucketSubmission<CubeVertex, CubeInstanceData>
            {
                VertexBuffer = _vb,
                VertexCount = (ushort)s_vertices.Length,
                IndexBuffer = _ib,
                IndexCount = (ushort)s_indices.Length,
                InstanceDataBuffer = _instanceData,
                InstanceBase = _instanceData.Append(instance),
                Pipeline = _pipeline,
                SharedResourceSet = _viewProjectionSet,
                ObjectResourceSet = _cubeResourceSet
            };

            _instanceData.End(renderContext.MainCommandList);

            bucket.Submit(ref submission, 0);
        }

        private static readonly CubeVertex[] s_vertices = new CubeVertex[]
        {
            // Top
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(-0.5f,0.5f,0.5f),
            // Bottom
            new CubeVertex(-0.5f,-0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            // Left
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            // Right
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            // Back
            new CubeVertex(0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,0.5f,-0.5f),
            new CubeVertex(-0.5f,-0.5f,-0.5f),
            new CubeVertex(0.5f,-0.5f,-0.5f),
            // Front
            new CubeVertex(-0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,0.5f,0.5f),
            new CubeVertex(0.5f,-0.5f,0.5f),
            new CubeVertex(-0.5f,-0.5f,0.5f),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        };

        internal readonly struct CubeVertex
        {
            public readonly SimpleVector3 Position;

            public CubeVertex(float x, float y, float z)
            {
                Position = new SimpleVector3(x, y, z);
            }

            public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));
        }

        internal readonly struct CubeInstanceData : IEquatable<CubeInstanceData>
        {
            public readonly RgbaFloat Color;
            public readonly Matrix4x4 Transform;

            public CubeInstanceData(ref RgbaFloat color, in Matrix4x4 transform)
            {
                Color = color;
                Transform = transform;
            }

            public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
                stride: 80, instanceStepRate: 1,
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Col1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Col2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Col3", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("Col4", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            public bool Equals(CubeInstanceData other)
            {
                return Color.Equals(other.Color) && Transform.Equals(other.Transform);
            }
        }
    }
}
