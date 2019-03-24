using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Graphics;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using Rectangle = NitroSharp.Primitives.Rectangle;

namespace NitroSharp.FreeTypePlayground
{
    class Program
    {
        const int FontSize = 28;

        static unsafe void Main(string[] args)
        {
            var options = new GraphicsDeviceOptions(true, null, true);
            options.PreferStandardClipSpaceYDirection = true;
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Sample Text"),
                options,
                GraphicsBackend.Direct3D11,
                out Sdl2Window window,
                out GraphicsDevice gd);

            const uint width = 512;
            const uint height = 512;
            CommandList cl = gd.ResourceFactory.CreateCommandList();

            var fontLib = new FontLibrary();
            FontFamily fontFamily = fontLib.RegisterFont("Fonts/NotoSansCJKjp-Regular.otf");
            FontFace font = fontFamily.GetFace(FontStyle.Regular);
            var g = font.GetGlyph('G', 22.3f);


            var bigFontSize = 40;
            var meowColor = new RgbaFloat(253 / 255.0f, 149 / 255.0f, 89 / 255.0f, 1.0f);

            var blue = new RgbaFloat(6 / 255.0f, 67 / 255.0f, 94 / 255.0f, 1.0f);
            //var layout = new TextLayout(
            //    new[]
            //    {
            //        TextRun.Regular("meow".AsMemory(), font, bigFontSize, meowColor),
            //        TextRun.Regular(" is a game about ".AsMemory(), font, FontSize, RgbaFloat.Black),
            //        TextRun.Regular("Increasing The Cats".AsMemory(), font, bigFontSize, RgbaFloat.Black),
            //        TextRun.Regular(".\nyou can summon as many cats as you want and watch them bounce and roll around and ".AsMemory(), font, FontSize, RgbaFloat.Black),
            //        TextRun.Regular("meow".AsMemory(), font, bigFontSize, meowColor),
            //        TextRun.Regular(" at you.\nplease enjoy your cat friends".AsMemory(), font, FontSize, RgbaFloat.Black)
            //    },
            //    new Size(400, 400)
            //);

            var layout = new TextLayout(
                new[]
                {
                    TextRun.Regular("Sample Text\n                          ".AsMemory(), font, FontSize, RgbaFloat.White),
                    TextRun.WithRubyText("西條".AsMemory(), "にしじょう".AsMemory(), font, FontSize, blue)
                },
                new Size(400, 600));

            var size = Unsafe.SizeOf<TextRun>();

            //var layout = new TextLayout(
            //    new[]
            //    {
            //        TextRun.MakeRegular("This text is rasterized with ".AsMemory(), font, FontSize, RgbaFloat.Black),
            //        TextRun.MakeRegular("FreeType".AsMemory(), font, bigFontSize, blue),
            //        TextRun.MakeRegular("\nGlyph images are cached on the GPU as needed\n".AsMemory(), font, FontSize, RgbaFloat.White),
            //        TextRun.MakeRegular("Only ".AsMemory(), font, FontSize, RgbaFloat.Yellow),
            //        TextRun.MakeRegular("one".AsMemory(), font, bigFontSize, meowColor),
            //        TextRun.MakeRegular(" draw call is required to render this text".AsMemory(), font, FontSize, RgbaFloat.Yellow)
            //    },
            //    new Size(400, 400)
            //);

            layout.EndLine(font);

            //string charset = File.ReadAllText("S:/noah-charset.utf8");
            string charset = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz西條にしじょう .,- \n'";

            var factory = gd.ResourceFactory;
            var shaderLibrary = new ShaderLibrary(gd);

            var designResolution = new Size(1280, 720);
            var projection = Matrix4x4.CreateOrthographicOffCenter(
               0, designResolution.Width, designResolution.Height, 0, 0, -1);
            var projectionBuffer = gd.CreateStaticBuffer(ref projection, BufferUsage.UniformBuffer);

            var mainBucket = new RenderBucket<int>(16);

            (Shader vs, Shader fs) = shaderLibrary.GetShaderSet("text");

            var vsLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("GlyphRects", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ArrayLayers", ResourceKind.TextureReadOnly, ShaderStages.Vertex)));

            var fsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Atlas", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            var pipeline = factory.CreateGraphicsPipeline(
                new GraphicsPipelineDescription(
                    BlendStateDescription.SingleAlphaBlend,
                    DepthStencilStateDescription.Disabled,
                    RasterizerStateDescription.CullNone,
                    PrimitiveTopology.TriangleList,
                    new ShaderSetDescription(
                        new[] { QuadVertex.LayoutDescription, InstanceData.LayoutDescription },
                        new[] { vs, fs }),
                    new[] { vsLayout, fsLayout },
                    gd.SwapchainFramebuffer.OutputDescription));

            DeviceBuffer rectsStaging = factory.CreateBuffer(new BufferDescription(
                (uint)(sizeof(Vector4) * 4096), BufferUsage.Staging));
            MappedResourceView<Vector4> rects = gd.Map<Vector4>(rectsStaging, MapMode.Write);

            Texture layersStaging = factory.CreateTexture(TextureDescription.Texture1D(
                    4096, 1, 1, PixelFormat.R8_UInt, TextureUsage.Staging));
            MappedResourceView<byte> arrayLayers = gd.Map<byte>(layersStaging, MapMode.Write, 0);

            var glyphMap = new Dictionary<GlyphCacheKey, ushort>();

            var atlas = new TextureAtlas(gd, width, height, layerCount: 64, PixelFormat.R8_UNorm);

            cl.Begin();
            atlas.Begin(clear: true);
            var pixels = new byte[16 * 1024];

            var rs = new Rectangle[charset.Length * 2];

            loop(0, FontSize);
            loop(charset.Length, 11);

            void loop(int start, int fontSize)
            {
                for (int i = start; i < start + charset.Length; i++)
                {
                    Glyph glyph = font.GetGlyph(charset[i - start], fontSize);
                    Size dimensions = glyph.BitmapSize;
                    if (dimensions.Height > 0)
                    {
                        glyph.Rasterize(font, pixels);
                        atlas.TryPackSprite<byte>(
                            pixels,
                            dimensions.Width,
                            dimensions.Height,
                            out uint layer,
                            out rs[i]);

                        arrayLayers[i] = (byte)layer;
                        var rect = rs[i];
                        rects[i] = new Vector4(
                            rect.Left,
                            rect.Top,
                            rect.Right,
                            rect.Bottom);

                        var key = new GlyphCacheKey(charset[i - start], fontSize);
                        glyphMap[key] = (ushort)i;
                    }
                }
            }

            font.Dispose();

            gd.Unmap(rectsStaging);
            gd.Unmap(layersStaging);
            atlas.End(cl);
            cl.End();
            gd.SubmitCommands(cl);

            DeviceBuffer rectBuffer = factory.CreateBuffer(new BufferDescription(
               (uint)(sizeof(Vector4) * 4096), BufferUsage.UniformBuffer));

            Texture arrayLayersBuffer = factory.CreateTexture(TextureDescription.Texture1D(
                    4096, 1, 1, PixelFormat.R8_UInt, TextureUsage.Sampled));

            cl.Begin();
            cl.CopyBuffer(rectsStaging, 0, rectBuffer, 0, rectBuffer.SizeInBytes);
            cl.CopyTexture(layersStaging, arrayLayersBuffer);

            var vsResourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(vsLayout, projectionBuffer, rectBuffer, arrayLayersBuffer));
            var fsResourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(fsLayout, atlas.Texture, gd.LinearSampler));

            var vb = new VertexBuffer<QuadVertex>(gd, cl,
                new[]
                {
                    new QuadVertex(new Vector2(-1, 1)),
                    new QuadVertex(new Vector2(1, 1)),
                    new QuadVertex(new Vector2(-1, -1)),
                    new QuadVertex(new Vector2(1, -1))
                });

            var ib = gd.CreateStaticBuffer(new ushort[] { 0, 1, 2, 2, 1, 3 }, BufferUsage.IndexBuffer);

            var pgs = layout.Glyphs;
            var glyphs = new ArrayBuilder<InstanceData>(100);
            for (int i = 0; i < layout.Glyphs.Length; i++)
            {
                ref readonly PositionedGlyph pg = ref pgs[i];
                if (char.IsWhiteSpace(pg.Character))
                {
                    continue;
                }

                ref readonly TextRun run = ref layout.TextRuns[pg.TextRunIndex];
                ref InstanceData data = ref glyphs.Add();
                int fontSize = run.FontSize;
                if (pg.IsRuby)
                {
                    fontSize = (int)(fontSize * 0.4f);
                }
                var key = new GlyphCacheKey(pg.Character, fontSize);
                int idx = glyphMap[key];
                data.GlyphIndex = idx;
                data.Origin = pg.Position;
                data.Color = run.Color.ToVector4();
            }

            var instanceData = new VertexBuffer<InstanceData>(gd, cl, glyphs.AsSpan());

            cl.End();
            gd.SubmitCommands(cl);

            while (window.Exists)
            {
                window.PumpEvents();
                if (!window.Exists) { break; }

                cl.Begin();
                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

                mainBucket.Begin();
                var submission = new RenderBucketSubmission<QuadVertex, InstanceData>
                {
                    Pipeline = pipeline,
                    SharedResourceSet = vsResourceSet,
                    ObjectResourceSet = fsResourceSet,
                    VertexBuffer = vb,
                    IndexBuffer = ib,
                    IndexBase = 0,
                    IndexCount = 6,
                    InstanceDataBuffer = instanceData,
                    InstanceBase = 0,
                    InstanceCount = (ushort)pgs.Length
                };

                mainBucket.Submit(ref submission, 0);
                mainBucket.End(cl);

                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
            }

            gd.WaitForIdle();
            cl.Dispose();
            atlas.Dispose();
            gd.Dispose();
        }

        internal struct QuadVertex : IEquatable<QuadVertex>
        {
            public Vector2 Position;

            public QuadVertex(Vector2 pos)
            {
                Position = pos;
            }

            public static readonly VertexLayoutDescription LayoutDescription = new VertexLayoutDescription(
                new VertexElementDescription("vs_Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            public bool Equals(QuadVertex other)
                => Position == other.Position;
        }

        internal struct InstanceData : IEquatable<InstanceData>
        {
            public Vector4 Color;
            public Vector2 Origin;
            public int GlyphIndex;
            private int _padding;

            public static VertexLayoutDescription LayoutDescription => new VertexLayoutDescription(
                stride: 32, instanceStepRate: 1,
                new VertexElementDescription("vs_Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                new VertexElementDescription("vs_Origin", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("vs_GlyphIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));

            public bool Equals(InstanceData other)
                => GlyphIndex == other.GlyphIndex;
        }
    }
}
