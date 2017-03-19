using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.NSScript.Execution;
using System;
using System.IO;

namespace ProjectHoppy
{
    public class Noah : Game
    {
        private ConcurrentContentManager _content;
        private readonly NSScriptInterpreter _nssInterpreter;
        private readonly HoppyNssImplementation _nssBuiltIns;
        private HoppyConfig _config;

        public Noah()
        {
            _nssBuiltIns = new HoppyNssImplementation(Entities);
            _nssInterpreter = new NSScriptInterpreter(new ScriptLocator(), _nssBuiltIns);

            AddStartupTask(Init);
        }

        private void Init()
        {
            _config = HoppyConfig.Read();
            _content = new ConcurrentContentManager(_config.ContentPath);

            _nssInterpreter.CreateMicrothread("nss/ch01_007_円山町殺人現場");
        }

        public override void OnGraphicsInitialized()
        {
            Window.Title = "Chaos;Hoppy";
            _content.InitContentLoaders(RenderContext.ResourceFactory);

            var typewriterProcessor = new TypewriterAnimationProcessor(RenderContext);
            Systems.RegisterSystem(typewriterProcessor);

            var renderSystem = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(renderSystem);
        }

        public override void Update(float deltaMilliseconds)
        {
            _nssInterpreter.Run(HaltCondition.PendingBuiltInCall);
            LogBuiltInCalls();
            _nssInterpreter.DispatchPendingBuiltInCalls();

            base.Update(deltaMilliseconds);
        }

        private void LogBuiltInCalls()
        {
#if DEBUG
            foreach (var call in _nssInterpreter.PendingBuiltInCalls)
            {
                Console.WriteLine($"Built-in call: {call}");
            }
#endif
        }

        private class ScriptLocator : IScriptLocator
        {
            public Stream Locate(string fileName)
            {
                return File.OpenRead("S:/ProjectHoppy/" + fileName);
            }
        }
    }
}
