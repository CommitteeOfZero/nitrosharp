using HoppyFramework;
using HoppyFramework.Audio;
using HoppyFramework.Content;
using Microsoft.Extensions.Logging;
using ProjectHoppy.Graphics;
using SciAdvNet.NSScript.Execution;
using System;
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
            var scriptLocator = new ScriptLocator(_config.ContentRoot);

            _n2system = new N2System(Entities);
            _nssInterpreter = new NSScriptInterpreter(scriptLocator, _n2system);
            _n2system.Interpreter = _nssInterpreter;
            _nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;

            //_nssInterpreter.CreateThread("nss/test.nss");
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

        public override void OnInitialized()
        {
            Window.Title = "Chaos;Hoppy";

            _content = new ContentManager(_config.ContentRoot);

            var textureLoader = new WicTextureLoader(RenderContext);
            var audioLoader = new FFmpegAudioLoader();
            _content.RegisterContentLoader(typeof(TextureAsset), textureLoader);
            _content.RegisterContentLoader(typeof(AudioStream), audioLoader);

            _n2system.SetContent(_content);

            var inputHandler = new InputHandler(_n2system);
            Systems.RegisterSystem(inputHandler);

            var animationSystem = new AnimationSystem();
            Systems.RegisterSystem(animationSystem);

            var typewriterProcessor = new TypewriterAnimationProcessor();
            Systems.RegisterSystem(typewriterProcessor);

            var audioSystem = new AudioSystem(AudioEngine, _content);
            Systems.RegisterSystem(audioSystem);

            var renderSystem = new RenderSystem(RenderContext, _content);
            Systems.RegisterSystem(renderSystem);

            renderSystem.LoadSharedResources();
        }

        public override void Update(float deltaMilliseconds)
        {
            var timeQuota = TimeSpan.MaxValue;
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
            private readonly string _nssFolder;

            public ScriptLocator(string contentRoot)
            {
                _nssFolder = Path.Combine(contentRoot, "nss");
            }

            public Stream Locate(string fileName)
            {
                return File.OpenRead(Path.Combine(_nssFolder, fileName.Replace("nss/", string.Empty)));
            }
        }
    }
}
