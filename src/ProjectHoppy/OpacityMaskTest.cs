using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy
{
    public class OpacityMaskTest : Game
    {
        private readonly ContentManager _content;

        public OpacityMaskTest()
        {
            _content = new ContentManager("S:/HoppyContent");
            
        }

        public override void OnInitialized()
        {
            _content.InitContentLoaders(RenderContext.ResourceFactory, null);

            var renderSystem = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(renderSystem);

            Entities.Create("effect")
                .WithComponent(new VisualComponent(VisualKind.MaskEffect, 0, 0, 800, 600, 0) { Opacity = 0.9f })
                .WithComponent(new MaskEffect
                {
                    TextureRef = "cg/bg/bg002_01_1_青空_a.jpg",
                    MaskRef = "cg/data/right3.png"
                });

            _content.StartLoading<Texture2D>("cg/bg/bg002_01_1_青空_a.jpg");
            //_content.StartLoading<Texture2D>("cg/data/円形中外.png");
            _content.StartLoading<Texture2D>("cg/data/right3.png");
        }
    }
}
