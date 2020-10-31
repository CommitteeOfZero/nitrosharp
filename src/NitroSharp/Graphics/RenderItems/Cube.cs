using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal readonly struct CubeVertex
    {
        public readonly Vector3 Position;

        public CubeVertex(float x, float y, float z)
            => Position = new Vector3(x, y, z);

        public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
            ),
            new VertexElementDescription(
                "vs_Color",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float4
            )
        );
    }

    internal sealed class Cube : RenderItem3D
    {
        private readonly Texture _texture;
        private readonly ViewProjection _vp;
        private Matrix4x4 _worldMatrix;

        public static ReadOnlySpan<CubeVertex> Vertices => new[]
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

        public static ushort[] Indices => new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        };

        public Cube(in ResolvedEntityPath path, int priority, Texture texture, ViewProjection vp)
            : base(in path, priority)
        {
            _texture = texture;
            _vp = vp;
        }

        public static Cube Load(
            in ResolvedEntityPath path,
            int priority,
            RenderContext renderCtx,
            params string[] texturePaths)
        {
            ContentManager content = renderCtx.Content;
            ResourceFactory rf = renderCtx.ResourceFactory;
            IEnumerable<Task<Texture>> tasks = texturePaths
                .Select(x => Task.Run(() => content.LoadTexture(x, staging: true)));

            Texture[] textures = Task.WhenAll(tasks).Result;
            Texture firstTex = textures.First();

            Texture dstTexture = rf.CreateTexture(TextureDescription.Texture2D(
                firstTex.Width, firstTex.Height,
                mipLevels: 1, arrayLayers: 1,
                firstTex.Format, TextureUsage.Sampled | TextureUsage.Cubemap
            ));

            CommandList cl = renderCtx.SecondaryCommandList;
            for (uint i = 0; i < textures.Length; i++)
            {
                cl.CopyTexture(
                    textures[i], srcX: 0, srcY: 0, srcZ: 0, srcMipLevel: 0, srcBaseArrayLayer: 0,
                    dstTexture, dstX: 0, dstY: 0, dstZ: 0, dstMipLevel: 0, dstBaseArrayLayer: i,
                    firstTex.Width, firstTex.Height, depth: 1, layerCount: 1
                );
            }

            var designResolution = renderCtx.DesignResolution;
            var view = Matrix4x4.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 3.0f,
                (float)designResolution.Width / designResolution.Height,
                0.1f,
                1000.0f
            );

            var vp = new ViewProjection(renderCtx.GraphicsDevice, view * projection);
            return new Cube(path, priority, dstTexture, vp);
        }

        protected override void Update(GameContext ctx)
        {
            _worldMatrix = Transform.GetMatrix(new Size(1, 1));
        }

        public override void Render(RenderContext ctx, bool assetsReady)
        {
            DrawBatch batch = ctx.MainBatch;
            CubeShaderResources shaderResources = ctx.ShaderResources.Cube;
            ViewProjection vp = _vp;
            Mesh<CubeVertex> mesh = ctx.Cubes.Append(Vertices);

            batch.UpdateBuffer(shaderResources.TransformBuffer, _worldMatrix);

            ctx.MainBatch.PushDraw(new Draw
            {
                Pipeline = shaderResources.Pipeline,
                ResourceBindings = new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        shaderResources.TextureLayout,
                        _texture,
                        ctx.GetSampler(FilterMode.Linear)
                    ),
                    new ResourceSetKey(shaderResources.TransformLayout, shaderResources.TransformBuffer.VdBuffer)
                ),
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, 0, (uint)Indices.Length)
            });
        }
    }
}
