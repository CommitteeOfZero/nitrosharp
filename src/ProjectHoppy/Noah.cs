using Microsoft.Extensions.Logging;
using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.NSScript.Execution;
using System;
using System.IO;

namespace ProjectHoppy
{
    public class Noah : Game
    {
        private ContentManager _content;
        private NSScriptInterpreter _nssInterpreter;
        private N2SystemImplementation _nssBuiltIns;
        private HoppyConfig _config;

        private ILogger _interpreterLog;

        public Noah()
        {
            AddStartupTask(Init);
        }

        private void Init()
        {
            SetupLogging();
            _config = HoppyConfig.Read();
            _content = new ContentManager(_config.ContentPath);

            _nssBuiltIns = new N2SystemImplementation(Entities, _content);
            _nssInterpreter = new NSScriptInterpreter(new ScriptLocator(_config.NssFolderPath), _nssBuiltIns);
            _nssBuiltIns.Interpreter = _nssInterpreter;
            _nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;

            //_nssInterpreter.CreateThread("nss/boot-logo.nss");
            _nssInterpreter.CreateThread("nss/ch01_007_円山町殺人現場");
        }

        private void OnBuiltInCallDispatched(object sender, BuiltInFunctionCall call)
        {
            _interpreterLog.LogInformation($"Built-in call: {call.ToString()}");
        }

        private void SetupLogging()
        {
            var loggerFactory = new LoggerFactory().AddConsole();
            _interpreterLog = loggerFactory.CreateLogger("Interpreter");
        }

        public override void OnGraphicsInitialized()
        {
            Window.Title = "Chaos;Hoppy";
            _content.InitContentLoaders(RenderContext.ResourceFactory, AudioEngine.ResourceFactory);

            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            var typewriterProcessor = new TypewriterAnimationProcessor(RenderContext);
            Systems.RegisterSystem(typewriterProcessor);

            var audioSystem = new AudioSystem(AudioEngine, _content);
            Systems.RegisterSystem(audioSystem);

            var renderSystem = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(renderSystem);
        }

        public override void Update(float deltaMilliseconds)
        {
            if (!_nssBuiltIns.Waiting)
            {
                _nssInterpreter.Run(TimeSpan.FromMilliseconds(8.0f));
            }
            base.Update(deltaMilliseconds);
        }

        private class ScriptLocator : IScriptLocator
        {
            private readonly string _root;

            public ScriptLocator(string root)
            {
                _root = root;
            }

            public Stream Locate(string fileName)
            {
                return File.OpenRead(Path.Combine(_root, fileName.Replace("nss/", string.Empty)));
            }
        }
    }
}
