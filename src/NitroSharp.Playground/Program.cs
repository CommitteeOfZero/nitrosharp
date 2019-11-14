using System;
using System.Diagnostics;
using NitroSharp.Graphics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using NitroSharp.Content;
using System.IO;

namespace NitroSharp.Playground
{
    class Program
    {
        private const string SampleBackground = "D:/CoZ/Games/Chaos;HEad/cg/bg/bg034_01_3_ネットカフェ37_a.jpg";
        //private const string SampleBackground = "D:/CoZ/Games/Chaos;HEad/testcg/white.png";

        static void Main(string[] args)
        {
            var options = new GraphicsDeviceOptions(false, null, true);
            options.PreferStandardClipSpaceYDirection = true;
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 800, 600, WindowState.Normal, "Sample Text"),
                options,
                GraphicsBackend.Direct3D11,
                out Sdl2Window window,
                out GraphicsDevice gd);

            CommandList cl = gd.ResourceFactory.CreateCommandList();

            var shaderLibrary = new ShaderLibrary(gd);

            var textureLoader = new WicTextureLoader(gd);
            using var stream = File.OpenRead(SampleBackground);
            Texture bg = textureLoader.LoadTexture(stream, staging: false);

            var factory = gd.ResourceFactory;

            var effects = new Effects(factory, shaderLibrary);

            var samplerDesc = SamplerDescription.Point;
            samplerDesc.AddressModeU = SamplerAddressMode.Clamp;
            samplerDesc.AddressModeV = SamplerAddressMode.Clamp;
            var sampler = factory.CreateSampler(ref samplerDesc);

            var sw = Stopwatch.StartNew();
            Texture result;
            using (var effect = new SinglePassEffect(factory, effects, bg, sampler, EffectKind.Grayscale))
            {
                cl.Begin();
                result = effect.Apply(cl);
                cl.End();
            }
            gd.SubmitCommands(cl);
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);

            var blitter1 = new TextureBlitter(
                gd,
                shaderLibrary,
                "blit",
                gd.ResourceFactory,
                gd.SwapchainFramebuffer.OutputDescription
            );

            var set1 = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
              blitter1.ResourceLayout, result, gd.PointSampler
            ));

            var stopwatch = Stopwatch.StartNew();
            long prevFrameTicks = 0;
            while (window.Exists)
            {
                long currentFrameTicks = stopwatch.ElapsedTicks;
                float deltaMilliseconds = (float)(currentFrameTicks - prevFrameTicks)
                    / Stopwatch.Frequency * 1000.0f;
                prevFrameTicks = currentFrameTicks;

                window.PumpEvents();
                if (!window.Exists) { break; }

                cl.Begin();

                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

                blitter1.Render(cl, set1);

                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
            }

            gd.WaitForIdle();
            bg.Dispose();
            textureLoader.Dispose();
            shaderLibrary.Dispose();
            cl.Dispose();
            gd.Dispose();
        }
    }
}
