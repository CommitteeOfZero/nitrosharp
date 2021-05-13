using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NitroSharp.Saving;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct CubeVertex
    {
        public readonly Vector3 Position;
        public float Opacity;

        public CubeVertex(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
            Opacity = 1.0f;
        }

        public static readonly VertexLayoutDescription LayoutDescription = new(
            new VertexElementDescription(
                "vs_Position",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float3
            ),
            new VertexElementDescription(
                "vs_Opacity",
                VertexElementSemantic.TextureCoordinate,
                VertexElementFormat.Float1
            )
        );
    }

    internal sealed class Cube : RenderItem
    {
        private readonly Texture _texture;
        private readonly string[] _texturePaths;

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
            new CubeVertex(-0.5f,-0.5f,0.5f)
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

        private Cube(in ResolvedEntityPath path, int priority, Texture texture, string[] texturePaths)
            : base(path, priority)
        {
            _texture = texture;
            _texturePaths = texturePaths;
        }

        public Cube(in ResolvedEntityPath path, in CubeSaveData saveData, RenderContext ctx)
            : base(path, saveData.Common)
        {
            _texture = CreateCubemap(ctx, saveData.TexturePaths);
            _texturePaths = saveData.TexturePaths;
        }

        public override EntityKind Kind => EntityKind.Cube;

        public static Cube Load(
            in ResolvedEntityPath path,
            int priority,
            RenderContext renderCtx,
            params string[] texturePaths)
        {
            Texture dstTexture = CreateCubemap(renderCtx, texturePaths);
            return new Cube(path, priority, dstTexture, texturePaths);
        }

        private static Texture CreateCubemap(RenderContext renderCtx, string[] texturePaths)
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

            GraphicsDevice gd = renderCtx.GraphicsDevice;
            CommandList cl = renderCtx.TransferCommands;
            for (uint i = 0; i < textures.Length; i++)
            {
                cl.CopyTexture(
                    textures[i], srcX: 0, srcY: 0, srcZ: 0, srcMipLevel: 0, srcBaseArrayLayer: 0,
                    dstTexture, dstX: 0, dstY: 0, dstZ: 0, dstMipLevel: 0, dstBaseArrayLayer: i,
                    firstTex.Width, firstTex.Height, depth: 1, layerCount: 1
                );
                // TODO: investigate why regular Dispose causes issues when it shouldn't.
                if (gd.BackendType == GraphicsBackend.Direct3D11)
                {
                    textures[i].Dispose();
                }
                else
                {
                    gd.DisposeWhenIdle(textures[i]);
                }
            }

            return dstTexture;
        }

        protected override void Update(GameContext ctx)
        {
        }

        public override void Render(RenderContext ctx, bool assetsReady)
        {
            DrawBatch batch = ctx.MainBatch;
            CubeShaderResources resources = ctx.ShaderResources.Cube;
            ViewProjection vp = ctx.PerspectiveViewProjection;

            Span<CubeVertex> vertices = stackalloc CubeVertex[Vertices.Length];
            Vertices.CopyTo(vertices);
            foreach (ref CubeVertex v in vertices)
            {
                v.Opacity = Color.A;
            }

            Mesh<CubeVertex> mesh = ctx.Cubes.Append(vertices);
            batch.UpdateBuffer(resources.TransformBuffer, Transform.GetMatrix());
            batch.PushDraw(new Draw
            {
                Pipeline = resources.Pipeline,
                ResourceBindings = new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        resources.TextureLayout,
                        _texture,
                        ctx.GetSampler(FilterMode.Linear)
                    ),
                    new ResourceSetKey(resources.TransformLayout, resources.TransformBuffer.VdBuffer)
                ),
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, 0, (uint)Indices.Length)
            });
        }

        public new CubeSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Common = base.ToSaveData(ctx),
            TexturePaths = _texturePaths
        };

        public override void Dispose()
        {
            _texture.Dispose();
        }
    }

    [Persistable]
    internal readonly partial struct CubeSaveData : IEntitySaveData
    {
        public RenderItemSaveData Common { get; init; }
        public string[] TexturePaths { get; init; }

        public EntitySaveData CommonEntityData => Common.EntityData;
    }
}
