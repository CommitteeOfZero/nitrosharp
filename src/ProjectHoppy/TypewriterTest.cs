using ProjectHoppy.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy
{
    public class TypewriterTest : Game
    {
        public TypewriterTest()
        {
            var typewriter = new TypewriterSystem();
            Systems.RegisterSystem(typewriter);

            var render = new RenderSystem(RenderContext);
            Systems.RegisterSystem(render);

            var text = new TextComponent { Text = "Hello world" };
            var succ = Entities.CreateEntity("succ").WithComponent(text).WithComponent(new VisualComponent());

        }

        public override void Run()
        {
            EnterLoop();
        }
    }
}
