using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NitroSharp.Content;
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

        //public BufferBindings(DeviceBuffer vertices, DeviceBuffer instanceData)
        //    : this() => (Vertices, InstanceData) = (vertices, instanceData);

        public BufferBindings(DeviceBuffer vertices, DeviceBuffer instanceData, DeviceBuffer indices)
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

        public ResourceBindings(ResourceSetKey rs0) : this()
            => ResourceSet0 = rs0;

        public ResourceBindings(ResourceSetKey rs0, ResourceSetKey rs1) : this()
            => (ResourceSet0, ResourceSet1) = (rs0, rs1);

        public ResourceBindings(ResourceSetKey rs0, ResourceSetKey rs1, ResourceSetKey rs2)
            : this() => (ResourceSet0, ResourceSet1, ResourceSet2) = (rs0, rs1, rs2);

        public ResourceBindings(
            ResourceSetKey rs0,
            ResourceSetKey rs1,
            ResourceSetKey rs2,
            ResourceSetKey rs3) : this()
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
        public readonly CommandList _drawCommands;
        private readonly Texture _screenshotTexture;
        private readonly Texture _secondaryFramebufferTexture;

        private readonly Queue<GpuBuffer<ushort>> _oldIndexBuffers;

        private Draw _lastDraw;

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
            _drawCommands = ResourceFactory.CreateCommandList();
            EffectCommandList = ResourceFactory.CreateCommandList();
            Content = contentManager;
            GlyphRasterizer = glyphRasterizer;
            ShaderLibrary = new ShaderLibrary(graphicsDevice);
            ViewProjection = new ViewProjection(GraphicsDevice, DesignResolution);
            Pipelines = new Pipelines(
                ResourceFactory,
                ShaderLibrary,
                SwapchainFramebuffer.OutputDescription,
                ViewProjection
            );

            ResourceSetCache = new ResourceSetCache(ResourceFactory);
            TextureCache = new TextureCache(GraphicsDevice);

            ResourceFactory rf = ResourceFactory;
            _secondaryFramebufferTexture = rf.CreateTexture(TextureDescription.Texture2D(
                SwapchainFramebuffer.Width, SwapchainFramebuffer.Height, mipLevels: 1,
                arrayLayers: 1, PixelFormat.B8_G8_R8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled
            ));
            SecondaryFramebuffer = rf.CreateFramebuffer(new FramebufferDescription(
                depthTarget: null,
                _secondaryFramebufferTexture
            ));

            _screenshotTexture = rf.CreateTexture(TextureDescription.Texture2D(
                SwapchainFramebuffer.Width, SwapchainFramebuffer.Height, mipLevels: 1,
                arrayLayers: 1, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled
            ));
            WhiteTexture = CreateWhiteTexture();

            QuadVertexBuffer = new GpuList<QuadVertex>(
                GraphicsDevice,
                BufferUsage.VertexBuffer,
                initialCapacity: 4
            );
            _oldIndexBuffers = new Queue<GpuBuffer<ushort>>();
            ResizeIndexBufferIfNecessary();
            Debug.Assert(QuadIndexBuffer != null);

            Text = new TextRenderContext(
                GraphicsDevice,
                ShaderLibrary,
                GlyphRasterizer,
                TextureCache,
                SwapchainFramebuffer.OutputDescription
            );
        }

        public Size DesignResolution { get; }
        public GraphicsDevice GraphicsDevice { get; }
        public ResourceFactory ResourceFactory { get; }
        public Swapchain MainSwapchain { get; }
        public Framebuffer SwapchainFramebuffer { get; }

        public Framebuffer SecondaryFramebuffer { get; }
        public CommandList EffectCommandList { get; }

        public ContentManager Content { get; }
        public GlyphRasterizer GlyphRasterizer { get; }
        public ViewProjection ViewProjection { get; }
        public ShaderLibrary ShaderLibrary { get; }
        public Pipelines Pipelines { get; }

        public ResourceSetCache ResourceSetCache { get; }
        public TextureCache TextureCache { get; }
        public Texture WhiteTexture { get; }

        public GpuList<QuadVertex> QuadVertexBuffer { get; }
        public GpuBuffer<ushort>? QuadIndexBuffer { get; private set; }

        public TextRenderContext Text { get; }

        public void BeginFrame(in FrameStamp frameStamp)
        {
            while (_oldIndexBuffers.TryDequeue(out GpuBuffer<ushort> buffer))
            {
                buffer.Dispose();
            }

            EffectCommandList.Begin();

            _drawCommands.Begin();
            _drawCommands.SetFramebuffer(SwapchainFramebuffer);
            _drawCommands.ClearColorTarget(0, RgbaFloat.Black);

            _transferCommands.Begin();
            QuadVertexBuffer.Begin();
            TextureCache.BeginFrame(frameStamp);
            ResourceSetCache.BeginFrame(frameStamp);
            _lastDraw = default;
            Text.BeginFrame();
            TextureCache.EndFrame(_transferCommands);
        }

        public void EndFrame()
        {
            Flush();

            EffectCommandList.End();
            GraphicsDevice.SubmitCommands(EffectCommandList);

            _drawCommands.End();


            Text.EndFrame(_transferCommands);
            ResourceSetCache.EndFrame();
            QuadVertexBuffer.End(_transferCommands);
            _transferCommands.End();

            GraphicsDevice.SubmitCommands(_transferCommands);
            GraphicsDevice.SubmitCommands(_drawCommands);
        }

        public void Present()
            => GraphicsDevice.SwapBuffers(MainSwapchain);

        public void PushQuad(
            QuadGeometry quad,
            Texture texture,
            Texture alphaMask,
            BlendMode blendMode,
            FilterMode filterMode)
        {
            ViewProjection vp = ViewProjection;
            PushQuad(quad, GetPipeline(blendMode),
                new ResourceBindings(
                    new ResourceSetKey(vp.ResourceLayout, vp.Buffer.VdBuffer),
                    new ResourceSetKey(
                        Pipelines.CommonResourceLayout,
                        texture,
                        alphaMask,
                        GetSampler(filterMode)
                    )
                )
            );
        }

        public void PushQuad(QuadGeometry quad, Pipeline pipeline, in ResourceBindings resources)
        {
            Debug.Assert(QuadIndexBuffer != null);
            uint oldQuadCount = QuadVertexBuffer.Count / 4u;
            GpuListSlice<QuadVertex> vertices = QuadVertexBuffer.Append(4);
            Span<QuadVertex> src = MemoryMarshal.CreateSpan(ref quad.TopLeft, 4);
            src.CopyTo(vertices.Data);
            ResizeIndexBufferIfNecessary();
            PushDraw(new Draw
            {
                Pipeline = pipeline,
                ResourceBindings = resources,
                BufferBindings = new BufferBindings(vertices.Buffer, QuadIndexBuffer.VdBuffer),
                Params = DrawParams.Indexed(0, 6 * oldQuadCount, 6)
            });
        }

        public void PushDraw(in Draw draw)
        {
            if (ReferenceEquals(draw.Pipeline, _lastDraw.Pipeline)
                && draw.ResourceBindings.Equals(_lastDraw.ResourceBindings)
                && draw.BufferBindings.Equals(_lastDraw.BufferBindings)
                && DrawParams.TryMerge(ref _lastDraw.Params, draw.Params))
            {
                return;
            }

            if (!(_lastDraw.Pipeline is null))
            {
                Flush();
            }
            _lastDraw = draw;
        }

        private void Flush()
        {
            if (_lastDraw.Pipeline is null) { return; }
            CommandList cl = _drawCommands;
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

        private Sampler GetSampler(FilterMode filterMode) => filterMode switch
        {
            FilterMode.Linear => GraphicsDevice.LinearSampler,
            FilterMode.Point => GraphicsDevice.PointSampler,
            _ => ThrowHelper.Unreachable<Sampler>()
        };

        private Pipeline GetPipeline(BlendMode blendMode) => blendMode switch
        {
            BlendMode.Alpha => Pipelines.AlphaBlend,
            BlendMode.Additive => Pipelines.AdditiveBlend,
            BlendMode.ReverseSubtractive => Pipelines.ReverseSubtractiveBlend,
            BlendMode.Multiplicative => Pipelines.MultiplicativeBlend,
            _ => ThrowHelper.Unreachable<Pipeline>()
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

        private void ResizeIndexBufferIfNecessary()
        {
            uint indicesNeeded = 6 * (QuadVertexBuffer.Capacity / 4u);
            if (QuadIndexBuffer == null || QuadIndexBuffer.Capacity < indicesNeeded)
            {
                if (QuadIndexBuffer is GpuBuffer<ushort> oldBuffer)
                {
                    _oldIndexBuffers.Enqueue(oldBuffer);
                }
                Span<ushort> quadIndices = stackalloc ushort[] { 0, 1, 2, 2, 1, 3 };
                var indices = new ushort[indicesNeeded];
                for (int i = 0; i < indicesNeeded; i++)
                {
                    int quad = i / 6;
                    int vertexInQuad = i % 6;
                    indices[i] = (ushort)(quadIndices[vertexInQuad] + 4 * quad);
                }
                QuadIndexBuffer = GpuBuffer<ushort>.CreateIndex(GraphicsDevice, indices);
            }
        }

        public void Dispose()
        {
            _transferCommands.Dispose();
            _drawCommands.Dispose();
            EffectCommandList.Dispose();
            Pipelines.Dispose();
            WhiteTexture.Dispose();
            _screenshotTexture.Dispose();
            QuadVertexBuffer.Dispose();
            QuadIndexBuffer?.Dispose();
            TextureCache.Dispose();
            ResourceSetCache.Dispose();
            ViewProjection.Dispose();
            SecondaryFramebuffer.Dispose();
            _secondaryFramebufferTexture.Dispose();
            ShaderLibrary.Dispose();
        }
    }

    internal sealed class ViewProjection : IDisposable
    {
        public ViewProjection(GraphicsDevice gd, Size designResolution)
        {
            var projection = Matrix4x4.CreateOrthographicOffCenter(
                left: 0, right: designResolution.Width,
                bottom: designResolution.Height, top: 0,
                zNearPlane: 0.0f, zFarPlane: -1.0f
            );
            ResourceFactory rf = gd.ResourceFactory;
            ResourceLayout = rf.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ViewProjection",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex
                )
            ));
            Buffer = GpuBuffer<Matrix4x4>.CreateUniform(gd, projection);
            ResourceSet = rf.CreateResourceSet(
                new ResourceSetDescription(ResourceLayout, Buffer.VdBuffer)
            );
        }

        public ResourceSet ResourceSet { get; }
        public ResourceLayout ResourceLayout { get; }
        public GpuBuffer<Matrix4x4> Buffer { get; }

        public void Update(GraphicsDevice gd, in Matrix4x4 vp)
        {
            Buffer.Update(gd, vp);
        }

        public void Dispose()
        {
            ResourceSet.Dispose();
            ResourceLayout.Dispose();
            Buffer.Dispose();
        }
    }
}
