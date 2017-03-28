using Microsoft.Extensions.Logging;
using ProjectHoppy.Content;
using ProjectHoppy.Graphics;
using SciAdvNet.NSScript.Execution;
using System;
using System.Diagnostics;
using System.IO;

namespace ProjectHoppy
{
    public class ChaosHead : Game
    {
        private ContentManager _content;
        private NSScriptInterpreter _nssInterpreter;
        private N2System _n2system;
        private HoppyConfig _config;

        private ILogger _interpreterLog;

        public ChaosHead()
        {
            AddStartupTask(Init);
        }

        private void Init()
        {
            SetupLogging();
            _config = HoppyConfig.Read();
            _content = new ContentManager(_config.ContentPath);

            _n2system = new N2System(Entities, _content);
            _nssInterpreter = new NSScriptInterpreter(new ScriptLocator(_config.NssFolderPath), _n2system);
            _n2system.Interpreter = _nssInterpreter;
            _nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;

            //_nssInterpreter.CreateThread("nss/boot-logo.nss");
            _nssInterpreter.CreateThread("nss/ch01_007_円山町殺人現場");
        }

        private void OnBuiltInCallDispatched(object sender, BuiltInFunctionCall call)
        {
            //_interpreterLog.LogInformation($"Built-in call: {call.ToString()}");
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

            var inputHandler = new InputHandler(_n2system);
            Systems.RegisterSystem(inputHandler);

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
            var timeQuota = TimeSpan.FromMilliseconds(6);
            TimeSpan elapsed = _nssInterpreter.Run(timeQuota);

            if (elapsed - timeQuota > TimeSpan.FromMilliseconds(4))
            {
                _interpreterLog.LogCritical(666, $"Interpreter execution time quota exceeded " +
                    $"(quota: {timeQuota.TotalMilliseconds} ms; elapsed: {elapsed.TotalMilliseconds} ms).");
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
