using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using System;
using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Graphics;
using System.Threading.Tasks;
using SciAdvNet.MediaLayer.Audio;
using SciAdvNet.MediaLayer.Audio.XAudio;
using System.IO;

namespace ProjectHoppy
{
    public class TypewriterTest : Game
    {
        private ConcurrentContentManager _content;
        private HoppyConfig _config;

        public TypewriterTest()
        {
            AddStartupTask(LoadConfig);
        }

        private void LoadConfig()
        {
            _config = HoppyConfig.Read();
            _content = new ConcurrentContentManager(_config.ContentPath);
        }

        public override void OnGraphicsInitialized()
        {
            _content.InitContentLoaders(RenderContext.ResourceFactory);

            var typewriterProcessor = new TypewriterAnimationProcessor(RenderContext);
            Systems.RegisterSystem(typewriterProcessor);

            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            var textInputHandler = new TextInputHandler();
            Systems.RegisterSystem(textInputHandler);

            var render = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(render);

            var visual = new VisualComponent { Kind = VisualKind.Text, X = 100, Width = 600, Height = 600, LayerDepth = 1, Color = RgbaValueF.White };
            var text = new TextComponent { Animated = true, Text = "According to all known laws of aviation, there is no way that a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway. Because bees don't care what humans think is impossible." };

            Entities.CreateEntity("text")
                .WithComponent(visual)
                .WithComponent(text);

            Entities.CreateEntity("bg")
                .WithComponent(new VisualComponent { Kind = VisualKind.Texture, Width = 800, Height = 640, LayerDepth = 0 })
                .WithComponent(new AssetComponent { AssetPath = "cg/bg/bg000_01_1_チャットサンプル.jpg" });

            _content.EnqueueWorkItem("cg/bg/bg000_01_1_チャットサンプル.jpg");

            var fade = new FloatAnimation
            {
                PropertySetter = (e, v) => e.GetComponent<VisualComponent>().Opacity = v,
                InitialValue = 0.0f,
                FinalValue = 1.0f,
                Duration = TimeSpan.FromSeconds(10)
            };

            //Entities.CreateEntity("rect1")
            //    .WithComponent(new VisualComponent(VisualKind.Rectangle, 0, 0, 100, 100, 0) { Color = RgbaValueF.Green })
            //    .WithComponent(fade);

            //Entities.CreateEntity("rect2")
            //    .WithComponent(new VisualComponent(VisualKind.Rectangle, 200, 200, 100, 100, 0) { Color = RgbaValueF.Red })
            //    .WithComponent(fade);


            var audioEngine = new XAudio2AudioEngine(16, 44100, 2);
            var src = audioEngine.ResourceFactory.CreateAudioSource();

            //var fileStream = _content._archive.GetEntry("sound/bgm/ch01.ogg").Open();
            ////var fileStream = File.OpenRead("Stay Alive.m4a");
            ////var memStream = new MemoryStream((int)fileStream.Length);
            ////fileStream.CopyTo(memStream);
            ////memStream.Position = 0;

            var fileStream = File.OpenRead(Path.Combine(_content.RootDirectory, "sound/bgm/ch03.ogg"));
            var audioStream = new FFmpegAudioStream(fileStream);

            src.SetStream(audioStream);
            //audioStream.Seek(TimeSpan.FromMilliseconds(6645));
            //184915
            audioStream.SetLoop(TimeSpan.FromMilliseconds(6645), TimeSpan.FromMilliseconds(184915));
            audioStream.Seek(TimeSpan.FromMilliseconds(179915));
            //audioStream.Seek(TimeSpan.FromMilliseconds(179915));
            src.Play();
        }
    }
}
