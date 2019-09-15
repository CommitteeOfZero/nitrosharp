using System;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Graphics;
using NitroSharp.Text;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace NitroSharp.FreeTypePlayground
{
    //[MemoryDiagnoser]
    //public class RasterizerBenchmark
    //{
    //    private GlyphRasterizer _rasterizer;
    //    private FontKey _fontKey;
    //    //private uint[] _indices;
    //    private GraphicsDevice _gd;
    //    private CommandList _cl;
    //    private TextLayoutBuilder _layout;
    //    private GpuCache<TextureLocation> _uvRectCache;
    //    private TextureCache _cache;

    //    [GlobalSetup]
    //    public void Setup()
    //    {
    //        _rasterizer = new GlyphRasterizer();
    //        _fontKey = _rasterizer.AddFont("Fonts/NotoSansCJKjp-Regular.otf");

    //        // _indices = new uint[256];
    //        FontData data = _rasterizer.GetFontData(_fontKey)!;
    //        //for (int i = 0; i < 256; i++)
    //        //{
    //        //    _indices[i] = data.GetGlyphIndex((char)('A' + i));
    //        //}

    //        var options = new GraphicsDeviceOptions(true, null, true);
    //        options.PreferStandardClipSpaceYDirection = true;
    //        _gd = GraphicsDevice.CreateD3D11(options);
    //        _cl = _gd.ResourceFactory.CreateCommandList();
    //        ResourceFactory factory = _gd.ResourceFactory;

    //        _uvRectCache = new GpuCache<TextureLocation>(
    //            _gd,
    //            TextureLocation.SizeInGpuBlocks,
    //            initialCapacity: 256
    //        );
    //        _cache = new TextureCache(_gd, _uvRectCache, initialLayerCount: 1);

    //        var layout = new TextLayoutBuilder(_rasterizer,
    //            new[]
    //            {
    //                TextRun.Regular("なんとか刀をくっつけようとしてみるけど、折れた部分を接着することはできても、完全に修復するのは不可能だった。これじゃもう価値なしだ。".AsMemory(), _fontKey, FontSize, RgbaFloat.White),
    //            },
    //            new Size(2000, 600)
    //        );

    //        layout.EndLine(data);

    //        _layout = layout;
    //    }

    //    PtFontSize FontSize = new PtFontSize(14);

    //    [GlobalCleanup]
    //    public void Cleanup()
    //    {
    //        _cache.Dispose();
    //        _uvRectCache.Dispose();
    //        _cl.Dispose();
    //        _gd.Dispose();
    //    }

    //    [Benchmark(Baseline = true)]
    //    public void RasterizeSingleThreaded()
    //    {
    //        TextLayoutBuilder layout = _layout;
    //        foreach (ref readonly GlyphRun run in layout.GlyphRuns)
    //        {
    //            ReadOnlySpan<PositionedGlyph> glyphs = layout.GetGlyphs(run.GlyphSpan);
    //            _rasterizer.RasterizeGlyphs(_fontKey, FontSize, glyphs);
    //        }
    //    }

    //    [Benchmark]
    //    public void RasterizeParallel()
    //    {
    //        TextLayoutBuilder layout = _layout;
    //        foreach (ref readonly GlyphRun run in layout.GlyphRuns)
    //        {
    //            ReadOnlySpan<PositionedGlyph> glyphs = layout.GetGlyphs(run.GlyphSpan);
    //            _rasterizer.RequestGlyphs(_fontKey, FontSize, glyphs, _cache);
    //        }

    //        _rasterizer.ResolveGlyphs(_cache).AsTask().Wait();
    //        // _rasterizer.RasterizeOutlinesParallel2(_fontKey, new PtFontSize(14), _indices);
    //    }

    //    //[Benchmark]
    //    //public void RasterizeSingleThreaded()
    //    //{
    //    //    _rasterizer.RasterizeGlyphs(_fontKey, new PtFontSize(14), _indices);
    //    //}

    //    //[Benchmark]
    //    //public void RasterizeParallel()
    //    //{
    //    //    _rasterizer.RasterizeGlyphsParallel(_fontKey, new PtFontSize(14), _indices)
    //    //        .Wait();
    //    //}

    //    //[Benchmark]
    //    //public void RasterizeParallel2()
    //    //{
    //    //    _rasterizer.RasterizeOutlinesParallel2(_fontKey, new PtFontSize(14), _indices);
    //    //}

    //    //[Benchmark]
    //    //public void RasterizeParallel3()
    //    //{
    //    //    _rasterizer.RasterizeOutlinesParallel3(_fontKey, new PtFontSize(14), _indices);
    //    //}
    //}

    class Program
    {
        private static readonly PtFontSize FontSize = new PtFontSize(28);

        static void Main()
        {
            var options = new GraphicsDeviceOptions(false, null, true);
            options.PreferStandardClipSpaceYDirection = true;
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Sample Text"),
                options,
                GraphicsBackend.Direct3D11,
                out Sdl2Window window,
                out GraphicsDevice gd);

            CommandList cl = gd.ResourceFactory.CreateCommandList();

            var shaderLibrary = new ShaderLibrary(gd);
            var designResolution = new Size(1280, 720);
            var projection = Matrix4x4.CreateOrthographicOffCenter(
               0, designResolution.Width, designResolution.Height, 0, 0, -1);
            var projectionBuffer = gd.CreateStaticBuffer(ref projection, BufferUsage.UniformBuffer);

            var mainBucket = new RenderBucket<int>(16);

            var textureCache = new TextureCache(gd, initialLayerCount: 4);

            var rasterizer = new GlyphRasterizer(enableOutlines: true);
            FontKey font = rasterizer.AddFont("Fonts/NotoSansCJKjp-Regular.otf");
            //FontKey font = rasterizer.AddFont("C:/Windows/Fonts/consola.ttf");
            FontKey comicItalic = rasterizer.AddFont("C:/Windows/Fonts/comicz.ttf");
            //FontKey font = comicItalic;
            var textRenderer = new TextRenderer(
                gd,
                shaderLibrary,
                rasterizer,
                textureCache,
                projectionBuffer
            );

            string txt = @"その答えは“僕を操作しているプレイヤー”っていうことになる。
だったらもっと上手にプレイしてほしいね。
でもアバターは文句を言えない。
『将軍』と僕が同一人物っていうのも、説明がつく。
言ってみればリーゼロッテみたいなものだ。
ナイトハルトとリーゼロッテは、決して同じ時間にバゼラードに存在できない。プレイヤーは僕ひとりなんだから。
それと同じように、西條拓巳と『将軍』もまた、同じ時間にこの世界には存在できないのかも。
あるいはバグだっていう可能性も考えられる。
バグと言えば、妙な女が現れたり殺人事件に遭遇したりするのもバグかもしれない。
Suck my motherfucking dick";

            string text = @"jLorem ipsum dolor sit amet, consectetur adipiscing elit. Sed ex mauris, ornare quis auctor eget, suscipit accumsan nunc. Nullam molestie vehicula vestibulum. Suspendisse malesuada scelerisque enim non placerat. Aliquam commodo libero vel tellus egestas posuere. Donec consequat interdum nibh. Curabitur ornare dolor at felis tincidunt, nec interdum ante maximus. Suspendisse potenti.

Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Phasellus eget tempus eros, nec egestas mauris. Sed ultricies, mauris at rhoncus iaculis, leo dolor lobortis mauris, et fringilla orci magna sit amet lacus. Fusce odio lectus, lacinia ut semper eu, accumsan sed ante. Nullam malesuada nisi et justo aliquam, in maximus nulla porta. Nulla volutpat, magna at venenatis tristique, libero sem lobortis odio, sed condimentum enim justo a dolor. Integer lectus eros, dictum id aliquam eget, condimentum et orci. Vivamus ut commodo diam, non viverra ex. Morbi rutrum sed sapien eget tincidunt. Praesent efficitur suscipit nibh. Mauris ac pellentesque massa. Nam molestie tempus leo sollicitudin ullamcorper.

Fusce non felis fringilla, facilisis turpis id, sodales tortor. Vestibulum ut eleifend lectus. Maecenas nibh neque, tincidunt ut egestas non, laoreet ut erat. Donec ut nibh ut orci congue placerat. Phasellus ut suscipit mi. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer eget tellus at eros lacinia consectetur a vel nulla.

Donec ac interdum massa. Duis et dignissim nunc, pulvinar posuere lorem. Nunc imperdiet gravida elit sit amet placerat. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Fusce eu urna scelerisque, mattis diam at, molestie diam. Morbi ac massa odio. Donec condimentum euismod nulla, nec fringilla nulla molestie eget. Aliquam volutpat quam neque, ut luctus dolor tristique et. Vestibulum euismod nunc ut nibh imperdiet rhoncus. Maecenas pretium orci ex, vel porta mi malesuada eget. Suspendisse vulputate tincidunt urna, eu dignissim ipsum fermentum ac. In et pharetra diam, eget mollis velit. Vivamus suscipit egestas orci, ac dignissim purus feugiat non. Vivamus mollis sit amet diam ac tincidunt. Mauris facilisis ut augue sit amet sodales.

Praesent eget sem turpis. Vestibulum ullamcorper egestas odio eget finibus. Sed mollis ligula odio, sed gravida sapien maximus a. Aliquam rhoncus dolor sit amet viverra laoreet. Aliquam erat volutpat. Morbi pellentesque nulla eu ornare euismod. Nunc tincidunt lacus vel velit ornare posuere. Sed purus purus, varius non elit sit amet, pharetra pulvinar est. Pellentesque interdum vehicula ante, eu mattis quam. Nullam sagittis augue nec quam volutpat, facilisis fringilla sem aliquet. Vestibulum et suscipit nibh. Morbi volutpat congue venenatis. Phasellus tempus leo non sapien tincidunt, vel pharetra metus faucibus. ";


            //txt = "を";

            ///text = "たてたたとにかくそんな状況だから手に入れるのはもう無理かもって思ってたんだけど、昨日＠ちゃんを見ていたら、最近になってその新刊がまんがだらけに再入荷したらしいっていう情報をたまたま入手したんだ。";

            text = "「あなたはタクのなんなの、って」";
            var layout = new TextLayout(rasterizer,
                new[]
                {
                    //TextRun.Regular(txt.AsMemory(), font, FontSize, RgbaFloat.Black, RgbaFloat.Black)
                    TextRun.Regular(text.AsMemory(), font, FontSize, RgbaFloat.White, RgbaFloat.Black),
                    //<RUBY text="しっぷうじんらい">疾風迅雷</RUBY>のナイトハルト"っていう異名だけでほとんどのプレイヤーには通じる。
                    //TextRun.WithRubyText("疾風迅雷".AsMemory(), "しっぷうじんらい".AsMemory(), font, FontSize, RgbaFloat.Black, RgbaFloat.Black),
                    //TextRun.Regular("のナイトハルトっていう異名だけでほとんどのプレイヤーには通じる。".AsMemory(), font, FontSize, RgbaFloat.Black, RgbaFloat.White)
                },
                new Size(1280, 720)
            );

            //var bb = layout.BoundingBox;

            //var bakColor = new RgbaFloat(25 / 255.0f, 29 / 255.0f, 34 / 255.0f, 1.0f);
            //bakColor = new RgbaFloat(247 / 255.0f, 249 / 255.0f,254 / 255.0f, 1.0f);
            var stopwatch = Stopwatch.StartNew();
            var sw = new Stopwatch();
            bool firstFrame = true;
            long frameId = 0;
            long prevFrameTicks = 0;
            float msElapsed = 0;
            while (window.Exists)
            {
                long currentFrameTicks = stopwatch.ElapsedTicks;
                float deltaMilliseconds = (float)(currentFrameTicks - prevFrameTicks)
                    / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                window.PumpEvents();
                if (!window.Exists) { break; }

                cl.Begin();
                mainBucket.Begin();

                textureCache.BeginFrame(new FrameStamp(frameId++, currentFrameTicks));
                textRenderer.BeginFrame();
                textRenderer.RequestGlyphs(layout);

                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, RgbaFloat.White);

                if (firstFrame)
                {
                    sw.Start();
                }
                textRenderer.ResolveGlyphs(layout);
                if (firstFrame)
                {
                    firstFrame = false;
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed.TotalMilliseconds);
                }

                textureCache.EndFrame(cl);
                textRenderer.EndFrame(mainBucket, cl);
                mainBucket.End(cl);

                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);

                msElapsed += deltaMilliseconds;
            }

            gd.WaitForIdle();
            textureCache.Dispose();
            textRenderer.Dispose();
            shaderLibrary.Dispose();
            projectionBuffer.Dispose();
            cl.Dispose();
            gd.Dispose();
        }
    }
}
