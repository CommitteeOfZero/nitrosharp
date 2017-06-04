using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Graphics;
using CommitteeOfZero.Nitro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommitteeOfZero.Nitro.Foundation.Content;

namespace HiDpiTest
{
    public class Test : Game
    {
        private RenderSystem _renderSystem;

        protected override ContentManager CreateContentManager()
        {
            var manager =  base.CreateContentManager();
            manager.RegisterContentLoader(typeof(TextureAsset), new WicTextureLoader(RenderContext));

            return manager;
        }

        protected override void RegisterSystems(IList<GameSystem> systems)
        {
            _renderSystem = new RenderSystem(RenderContext, Content);
            systems.Add(_renderSystem);
        }

        protected override void SetParameters(GameParameters parameters)
        {
            parameters.WindowWidth = 1280;
            parameters.WindowHeight = 720;
        }

        public override void LoadCommonResources()
        {
            _renderSystem.LoadCommonResources();

            //Entities.Create("rect")
            //    .WithComponent(new RectangleVisual(100, 100, RgbaValueF.Green, 1.0f, 0));
            Entities.Create("rect")
                .WithComponent(new Sprite(Content.Get<TextureAsset>("rect.png"), null, 1.0f, 0));
        }
    }
}
