using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy
{
    public class TypewriterTest : Game
    {
        public TypewriterTest()
        {
            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            var typewriterProcessor = new TypewriterAnimationProcessor(RenderContext);
            Systems.RegisterSystem(typewriterProcessor);

            var render = new RenderSystem(RenderContext);
            Systems.RegisterSystem(render);

            var visual = new VisualComponent { X = 100, Width = 600, Height = 600 };
            var text = new TextComponent { Text = "According to all known laws of aviation, there is no way that a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway. Because bees don't care what humans think is impossible." };

            Entities.CreateEntity("text")
                .WithComponent(visual)
                .WithComponent(text);

            //var animation = new ColorAnimation
            //{
            //    PropertySetter = (e, v) => e.GetComponent<TextComponent>().CurrentGlyphColor = v,
            //    InitialValue = new RgbaValueF(0, 0, 0, 0),
            //    FinalValue = RgbaValueF.Red,
            //    Duration = TimeSpan.FromSeconds(0.8)
            //};

            //var effect = new FloatAnimation
            //{
            //    PropertySetter = (e, v) => e.GetComponent<TextComponent>().CurrentGlyph = v,
            //    FinalValue = "Hello world".Length,
            //    Duration = TimeSpan.FromSeconds(3)
            //};

        }

        public override void Run()
        {
            EnterLoop();
        }
    }
}
