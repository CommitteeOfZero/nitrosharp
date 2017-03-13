using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using System;
using System.Collections.Generic;
using System.Text;
using ProjectHoppy.Content;
using SciAdvNet.MediaLayer.Graphics;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public class TypewriterTest : Game
    {
        private readonly ZipContentManager _content;

        public TypewriterTest()
        {
            _content = new ZipContentManager("S:/ProjectHoppy/Content.zip");

            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            //Task.Run(() => Content.Load<Texture2D>("cg/bg/bg000_01_1_チャットサンプル.jpg"));

        }

        public override async Task Initialize()
        {
            var init = Task.Run(() =>
            {
                _content.PreloadToc();

            });

            var baseInit = base.Initialize();
            await Task.WhenAll(init, baseInit);

            _content.InitContentLoaders(RenderContext.ResourceFactory);

            var typewriterProcessor = new TypewriterAnimationProcessor(RenderContext);
            Systems.RegisterSystem(typewriterProcessor);

            var render = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(render);

            var visual = new VisualComponent { Kind = VisualKind.Text, X = 100, Width = 600, Height = 600, LayerDepth = 1 };
            var text = new TextComponent { Text = "According to all known laws of aviation, there is no way that a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway. Because bees don't care what humans think is impossible." };

            Entities.CreateEntity("text")
                .WithComponent(visual)
                .WithComponent(text);

            Entities.CreateEntity("bg")
                .WithComponent(new VisualComponent { Kind = VisualKind.Texture, Width = 800, Height = 640, LayerDepth = 0 })
                .WithComponent(new AssetComponent { AssetPath = "cg/bg/bg000_01_1_チャットサンプル.jpg" });

            _content.EnqueueWorkItem("cg/bg/bg000_01_1_チャットサンプル.jpg");
        }
    }
}
