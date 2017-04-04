using HoppyFramework;
using HoppyFramework.Content;
using ProjectHoppy.Graphics;
using System;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public class ShaderEffectSample : Game
    {
        private ContentManager _content;

        public override void OnInitialized()
        {
            _content = new ContentManager("S:/HoppyContent/CH");

            var textureLoader = new WicTextureLoader(RenderContext);
            _content.RegisterContentLoader(typeof(TextureAsset), textureLoader);

            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            var graphics = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(graphics);
            PrepareScene();
        }

        private void PrepareScene()
        {
            _content.StartLoading<TextureAsset>("cg/bg/bg020_01_3_渋谷路地裏_a.jpg");

            Entities.Create("bg")
                .WithComponent(new VisualComponent(VisualKind.Texture, 0, 0, 800, 600, 0))
                .WithComponent(new TextureComponent { AssetRef = "cg/bg/bg020_01_3_渋谷路地裏_a.jpg" });

            var rect = new VisualComponent(VisualKind.Rectangle, 0, 0, 800, 600, 20000) { Color = RgbaValueF.Black };
            var animation = new FloatAnimation
            {
                TargetComponent = rect,
                PropertyGetter = c => (c as VisualComponent).Opacity,
                PropertySetter = (c, v) => (c as VisualComponent).Opacity = v,
                Duration = TimeSpan.FromSeconds(5),
                InitialValue = 0.0f,
                FinalValue = 1.0f,
            };

            Entities.Create("fade").WithComponent(rect).WithComponent(animation);

            //var effect = new DissolveTransition
            //{
            //    Texture = "cg/bg/bg020_01_3_渋谷路地裏_a.jpg",
            //    AlphaMask = "cg/data/right3.png"
            //};

            //var animation = new FloatAnimation
            //{
            //    TargetComponent = effect,
            //    PropertyGetter = c => (c as DissolveTransition).Opacity,
            //    PropertySetter = (c, v) => (c as DissolveTransition).Opacity = v,
            //    InitialValue = 0.0f,
            //    FinalValue = 1.0f,
            //    Duration = TimeSpan.FromSeconds(1)
            //};

            //Entities.Create("effect")
            //    .WithComponent(new VisualComponent(VisualKind.DissolveTransition, 0, 0, 800, 600, 0))
            //    .WithComponent(effect).WithComponent(animation);

            //_content.StartLoading<TextureAsset>("cg/bg/bg020_01_3_渋谷路地裏_a.jpg");
            //_content.StartLoading<TextureAsset>("cg/data/right3.png");
        }
    }
}
