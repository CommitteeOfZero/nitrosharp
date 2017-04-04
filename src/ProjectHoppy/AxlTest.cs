using HoppyFramework;
using ProjectHoppy.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHoppy
{
    public class AxlTest : Game
    {
        public override async void OnInitialized()
        {
            var anim = new AnimationSystem();
            Systems.RegisterSystem(anim);

            var r = new RenderSystem(RenderContext, new HoppyFramework.Content.ContentManager());
            Systems.RegisterSystem(r);

            var visual = new VisualComponent(VisualKind.Rectangle, 0, 0, 50, 50, 0) { Color = RgbaValueF.Green };
            var rect = Entities.Create("rect").WithComponent(visual);

            var move = new FloatAnimation
            {
                TargetComponent = visual,
                PropertyGetter = c => (c as VisualComponent).Y,
                PropertySetter = (c, v) => (c as VisualComponent).Y = v,
                Duration = TimeSpan.FromSeconds(3),
                InitialValue = 0,
                FinalValue = 500,
                TimingFunction = TimingFunction.QuarticEaseOut
            };

            await Task.Delay(100);
            rect.AddComponent(move);
        }
    }
}
