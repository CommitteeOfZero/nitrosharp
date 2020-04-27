using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NitroSharp.Content;
using NitroSharp.Graphics.Core;
using NitroSharp.Text;
using Veldrid;

#nullable enable

namespace NitroSharp.Graphics
{
    internal enum BlendMode : byte
    {
        Alpha,
        Additive,
        ReverseSubtractive,
        Multiplicative
    }

    internal enum FilterMode : byte
    {
        Point,
        Linear
    }

    internal struct Draw
    {
        public Pipeline Pipeline;
        public ResourceBindings ResourceBindings;
        public BufferBindings BufferBindings;
        public DrawParams Params;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct DrawParams
    {
        public readonly DrawMethod Method;
        public readonly (uint start, uint count) Vertices;
        public readonly (uint start, uint count) Indices;
        public readonly (uint start, uint count) Instances;

        public bool IsIndexed => Method switch
        {
            DrawMethod.DrawIndexed => true,
            DrawMethod.DrawIndexedInstanced => true,
            _ => false
        };

        public bool IsInstanced => Method switch
        {
            DrawMethod.DrawInstanced => true,
            DrawMethod.DrawIndexedInstanced => true,
            _ => false
        };

        public DrawParams(
            uint vertexBase,
            uint vertexCount,
            uint indexBase,
            uint indexCount,
            uint instanceBase,
            uint instanceCount)
        {
            Vertices = (vertexBase, vertexCount);
            Indices = (indexBase, indexCount);
            Instances = (instanceBase, instanceCount);
            Method = (Indices, Instances) switch
            {
                ((0, 0), (0, 1)) => DrawMethod.Draw,
                ((0, 0), _) => DrawMethod.DrawInstanced,
                (_, (0, 1)) => DrawMethod.DrawIndexed,
                _ => DrawMethod.DrawIndexedInstanced
            };
        }

        public static DrawParams Regular(
            uint vertexBase,
            uint vertexCount,
            uint instanceBase = 0,
            uint instanceCount = 1)
            => new DrawParams(vertexBase, vertexCount, 0, 0, instanceBase, instanceCount);

        public static DrawParams Indexed(
            uint vertexBase,
            uint indexBase,
            uint indexCount,
            uint instanceBase = 0,
            uint instanceCount = 1)
            => new DrawParams(vertexBase, 0, indexBase, indexCount, instanceBase, instanceCount);

        public static bool CanMerge(in DrawParams a, in DrawParams b)
        {
            if (a.Method != b.Method) { return false; }
            return a.Method switch
            {
                DrawMethod.Draw => areConsecutive(a.Vertices, b.Vertices),
                DrawMethod.DrawInstanced => areConsecutive(a.Vertices, b.Vertices)
                                         && areConsecutive(a.Instances, b.Instances),
                DrawMethod.DrawIndexed => a.Vertices.start == b.Vertices.start
                                       && areConsecutive(a.Indices, b.Indices),
                _ => a.Vertices.start == b.Vertices.start
                                      && areConsecutive(a.Indices, b.Indices)
                                      && areConsecutive(a.Instances, b.Instances)
            };

            static bool areConsecutive(
                (uint start, uint count) a,
                (uint start, uint count) b)
                => b.start == a.start + a.count;
        }

        public static bool TryMerge(ref DrawParams cur, in DrawParams next)
        {
            if (CanMerge(cur, next))
            {
                cur = Merge(cur, next);
                return true;
            }
            return false;
        }

        public static DrawParams Merge(in DrawParams a, in DrawParams b)
        {
            Debug.Assert(CanMerge(a, b));
            return new DrawParams(
                a.Vertices.start,
                a.Vertices.count + b.Vertices.count,
                a.Indices.start,
                a.Indices.count + b.Indices.count,
                a.Instances.start,
                a.IsInstanced ? a.Instances.count + b.Instances.count : 1
            );
        }
    }

    internal enum DrawMethod
    {
        Draw,
        DrawIndexed,
        DrawInstanced,
        DrawIndexedInstanced
    }

    internal readonly struct BufferBindings : IEquatable<BufferBindings>
    {
        public readonly DeviceBuffer? Vertices;
        public readonly DeviceBuffer? InstanceData;
        public readonly DeviceBuffer? Indices;

        public BufferBindings(DeviceBuffer vertices) : this()
            => Vertices = vertices;

        public BufferBindings(DeviceBuffer vertices, DeviceBuffer indices)
            : this() => (Vertices, Indices) = (vertices, indices);

        public BufferBindings(
            DeviceBuffer vertices,
            DeviceBuffer instanceData,
            DeviceBuffer? indices = null)
            => (Vertices, InstanceData, Indices) = (vertices, instanceData, indices);

        public bool Equals(BufferBindings other)
        {
            return ReferenceEquals(Vertices, other.Vertices)
                && ReferenceEquals(InstanceData, other.InstanceData)
                && ReferenceEquals(Indices, other.Indices);
        }
    }

    internal readonly struct ResourceBindings : IEquatable<ResourceBindings>
    {
        public readonly ResourceSetKey? ResourceSet0;
        public readonly ResourceSetKey? ResourceSet1;
        public readonly ResourceSetKey? ResourceSet2;
        public readonly ResourceSetKey? ResourceSet3;

        public ResourceBindings(
            ResourceSetKey rs0,
            ResourceSetKey? rs1 = null,
            ResourceSetKey? rs2 = null,
            ResourceSetKey? rs3 = null)
        {
            ResourceSet0 = rs0;
            ResourceSet1 = rs1;
            ResourceSet2 = rs2;
            ResourceSet3 = rs3;
        }

        public bool Equals(ResourceBindings other)
        {
            return ResourceSet0.Equals(other.ResourceSet0)
                && ResourceSet1.Equals(other.ResourceSet1)
                && ResourceSet2.Equals(other.ResourceSet2)
                && ResourceSet3.Equals(other.ResourceSet3);
        }
    }

    internal sealed class RenderContext : IDisposable
    {
        private readonly CommandList _transferCommands;
        private readonly CommandList _drawCommands;

        private readonly Texture _screenshotTexture;
        private readonly Texture _secondaryFramebufferTexture;

        private readonly MeshList<QuadVertex> _quads;
        private readonly MeshList<CubeVertex> _cubes;

        private Draw _lastDraw;
        private CommandList? _lastCommandList;

        public RenderContext(
            Configuration gameConfiguration,
            GraphicsDevice graphicsDevice,
            Swapchain swapchain,
            ContentManager contentManager,
            GlyphRasterizer glyphRasterizer)
        {
            DesignResolution = new Size(
                (uint)gameConfiguration.WindowWidth,
                (uint)gameConfiguration.WindowHeight
            );
            GraphicsDevice = graphicsDevice;
            ResourceFactory = graphicsDevice.ResourceFactory;
            MainSwapchain = swapchain;
            SwapchainFramebuffer = swapchain.Framebuffer;
            _transferCommands = ResourceFactory.CreateCommandList();
            _transferCommands.Name = "Transfer commands";
            _drawCommands = ResourceFactory.CreateCommandList();
            _drawCommands.Name = "Draw commands (primary)";
            SecondaryCommandList = ResourceFactory.CreateCommandList();
            SecondaryCommandList.Name = "Secondary";
            Content = contentManager;
            GlyphRasterizer = glyphRasterizer;
            ShaderLibrary = new ShaderLibrary(graphicsDevice);
            ViewProjection = ViewProjection.CreateOrtho(GraphicsDevice, DesignResolution);
            ShaderResources = new ShaderResources(
                graphicsDevice,
                ShaderLibrary,
                SwapchainFramebuffer.OutputDescription,
                ViewProjection
            );

            ResourceSetCache = new ResourceSetCache(ResourceFactory);
            TextureCache = new TextureCache(GraphicsDevice);

            ResourceFactory rf = ResourceFactory;
            _secondaryFramebufferTexture = rf.CreateTexture(TextureDescription.Texture2D(
                SwapchainFramebuffer.Width, SwapchainFramebuffer.Height,
                mipLevels: 1, arrayLayers: 1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled
            ));
            SecondaryFramebuffer = rf.CreateFramebuffer(new FramebufferDescription(
                depthTarget: null,
                _secondaryFramebufferTexture
            ));

            _screenshotTexture = rf.CreateTexture(TextureDescription.Texture2D(
                SwapchainFramebuffer.Width, SwapchainFramebuffer.Height,
                mipLevels: 1, arrayLayers: 1,
                PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.Staging
            ));
            WhiteTexture = CreateWhiteTexture();

            _quads = new MeshList<QuadVertex>(
                graphicsDevice,
                new MeshDescription(QuadGeometry.Indices, verticesPerMesh: 4),
                initialCapacity: 512
            );
            _cubes = new MeshList<CubeVertex>(
                graphicsDevice,
                new MeshDescription(Cube.Indices, verticesPerMesh: 24),
                initialCapacity: 1
            );

            Text = new TextRenderContext(
                GraphicsDevice,
                GlyphRasterizer,
                TextureCache
            );
        }

        public MeshList<QuadVertex> Quads => _quads;

        public Size DesignResolution { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ResourceFactory ResourceFactory { get; }
        public Swapchain MainSwapchain { get; }
        public Framebuffer SwapchainFramebuffer { get; }
        public Framebuffer SecondaryFramebuffer { get; }

        public CommandList DrawCommands => _drawCommands;
        public CommandList SecondaryCommandList { get; }

        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public ViewProjection ViewProjection { get; }
        public ShaderLibrary ShaderLibrary { get; }
        public ShaderResources ShaderResources { get; }

        public ResourceSetCache ResourceSetCache { get; }
        public TextureCache TextureCache { get; }
        public Texture WhiteTexture { get; }

        public Texture ScreenshotTexture => _screenshotTexture;

        public TextRenderContext Text { get; }

        public DrawBatch BeginBatch(RenderTarget renderTarget)
        {
            return new DrawBatch(this, SecondaryCommandList, renderTarget);
        }

        public void BeginFrame(in FrameStamp frameStamp)
        {
            _drawCommands.Begin();
            _drawCommands.SetFramebuffer(SwapchainFramebuffer);
            _drawCommands.ClearColorTarget(0, RgbaFloat.Black);

            SecondaryCommandList.Begin();

            _quads.Begin();
            _cubes.Begin();
            TextureCache.BeginFrame(frameStamp);
            ResourceSetCache.BeginFrame(frameStamp);
            Text.BeginFrame();

            _transferCommands.Begin();
            TextureCache.EndFrame(_transferCommands);
            _lastDraw = default;
        }

        public void EndFrame()
        {
            if (_lastCommandList is CommandList lastCl)
            {
                Flush(lastCl);
            }

            SecondaryCommandList.End();
            _drawCommands.End();
            ResourceSetCache.EndFrame();

            Text.EndFrame(_transferCommands);
            _quads.End(_transferCommands);
            _cubes.End(_transferCommands);
            _transferCommands.End();

            GraphicsDevice.SubmitCommands(_transferCommands);
            GraphicsDevice.SubmitCommands(SecondaryCommandList);
            GraphicsDevice.SubmitCommands(_drawCommands);
        }

        public void Present()
            => GraphicsDevice.SwapBuffers(MainSwapchain);

        public void PushQuad(
            CommandList commandList,
            QuadGeometry quad,
            Texture texture,
            Texture alphaMask,
            BlendMode blendMode,
            FilterMode filterMode)
        {
            ViewProjection vp = ViewProjection;
            PushQuad(commandList, quad, ShaderResources.Quad.GetPipeline(blendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        ShaderResources.Quad.ResourceLayout,
                        texture,
                        alphaMask,
                        GetSampler(filterMode)
                    )
                )
            );
        }

        public void PushQuad(
            CommandList commandList,
            QuadGeometry quad,
            Pipeline pipeline,
            in ResourceBindings resources)
        {
            Mesh<QuadVertex> mesh = _quads.Append(MemoryMarshal.CreateSpan(ref quad.TopLeft, 4));
            PushDraw(commandList, new Draw
            {
                Pipeline = pipeline,
                ResourceBindings = resources,
                BufferBindings = new BufferBindings(mesh.Vertices.Buffer, mesh.Indices.Buffer),
                Params = DrawParams.Indexed(0, mesh.IndexBase, 6)
            });
        }

        public void PushDraw(CommandList commandList, in Draw draw)
        {
            if (_lastCommandList is CommandList lastCL && commandList != lastCL)
            {
                Flush(lastCL);
            }
            else
            {
                if (ReferenceEquals(draw.Pipeline, _lastDraw.Pipeline)
                    && draw.ResourceBindings.Equals(_lastDraw.ResourceBindings)
                    && draw.BufferBindings.Equals(_lastDraw.BufferBindings)
                    && DrawParams.TryMerge(ref _lastDraw.Params, draw.Params))
                {
                    return;
                }

                if (_lastDraw.Pipeline is object && _lastCommandList is object)
                {
                    Flush(_lastCommandList);
                }
            }

            _lastCommandList = commandList;
            _lastDraw = draw;
        }

        private void Flush(CommandList commandList)
        {
            if (_lastDraw.Pipeline is null) { return; }
            CommandList cl = commandList;
            cl.SetPipeline(_lastDraw.Pipeline);
            ref BufferBindings buffers = ref _lastDraw.BufferBindings;
            if (buffers.Vertices is DeviceBuffer vertices)
            {
                cl.SetVertexBuffer(0, vertices);
            }
            if (buffers.InstanceData is DeviceBuffer instanceData)
            {
                cl.SetVertexBuffer(1, instanceData);

            }
            if (buffers.Indices is DeviceBuffer indices)
            {
                cl.SetIndexBuffer(indices, IndexFormat.UInt16);
            }

            ref ResourceBindings resources = ref _lastDraw.ResourceBindings;
            setResources(cl, 0, resources.ResourceSet0);
            setResources(cl, 1, resources.ResourceSet1);
            setResources(cl, 2, resources.ResourceSet2);
            setResources(cl, 3, resources.ResourceSet3);

            void setResources(CommandList cl, uint slot, ResourceSetKey? rsKeyOpt)
            {
                if (rsKeyOpt is ResourceSetKey rsKey)
                {
                    ResourceSet rs = ResourceSetCache.GetResourceSet(rsKey);
                    cl.SetGraphicsResourceSet(slot, rs);
                }
            }

            ref DrawParams p = ref _lastDraw.Params;
            if (p.IsIndexed)
            {
                cl.DrawIndexed(
                    p.Indices.count,
                    p.Instances.count,
                    p.Indices.start,
                    (int)p.Vertices.start,
                    p.Instances.start
                );
            }
            else
            {
                cl.Draw(
                    p.Vertices.count,
                    p.Instances.count,
                    p.Vertices.start,
                    p.Instances.start
                );
            }
        }

        public Sampler GetSampler(FilterMode filterMode) => filterMode switch
        {
            FilterMode.Linear => GraphicsDevice.LinearSampler,
            FilterMode.Point => GraphicsDevice.PointSampler,
            _ => ThrowHelper.Unreachable<Sampler>()
        };

        private Texture CreateWhiteTexture()
        {
            var textureDesc = TextureDescription.Texture2D(
                width: 1, height: 1, mipLevels: 1, arrayLayers: 1,
                PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging
            );
            Texture stagingWhite = ResourceFactory.CreateTexture(ref textureDesc);
            MappedResourceView<RgbaByte> pixels = GraphicsDevice.Map<RgbaByte>(
                stagingWhite, MapMode.Write
            );
            pixels[0] = RgbaByte.White;
            GraphicsDevice.Unmap(stagingWhite);

            textureDesc.Usage = TextureUsage.Sampled;
            Texture texture = ResourceFactory.CreateTexture(ref textureDesc);

            _transferCommands.Begin();
            _transferCommands.CopyTexture(stagingWhite, texture);
            _transferCommands.End();
            GraphicsDevice.SubmitCommands(_transferCommands);
            stagingWhite.Dispose();
            return texture;
        }

        public void Dispose()
        {
            _lastDraw = default;
            _lastCommandList = null;
            _transferCommands.Dispose();
            _drawCommands.Dispose();
            SecondaryCommandList.Dispose();
            ShaderResources.Dispose();
            WhiteTexture.Dispose();
            _screenshotTexture.Dispose();
            Text.Dispose();
            _quads.Dispose();
            _cubes.Dispose();
            TextureCache.Dispose();
            ResourceSetCache.Dispose();
            ViewProjection.Dispose();
            SecondaryFramebuffer.Dispose();
            _secondaryFramebufferTexture.Dispose();
            ShaderLibrary.Dispose();
        }
    }
}
