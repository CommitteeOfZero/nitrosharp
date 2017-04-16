using MoeGame.Framework;
using MoeGame.Framework.Audio;
using MoeGame.Framework.Content;
using Microsoft.Extensions.Logging;
using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.NsScript.Execution;
using System;
using CommitteeOfZero.NsScript;
using System.Collections.Generic;

namespace CommitteeOfZero.Nitro
{
    public partial class NitroGame : Game
    {
        private readonly NitroConfiguration _configuration;
        private RenderSystem _renderSystem;
        private NsScriptInterpreter _nssInterpreter;
        private NitroCore _nitroCore;

        private ILogger _interpreterLog;

        public NitroGame(NitroConfiguration configuration)
        {
            _configuration = configuration;
            SetupLogging();
        }

        protected override void SetParameters(GameParameters parameters)
        {
            parameters.WindowWidth = _configuration.WindowWidth;
            parameters.WindowHeight = _configuration.WindowHeight;
            parameters.WindowTitle = _configuration.WindowTitle;
        }

        protected override void RegisterStartupTasks(IList<Action> tasks)
        {
            tasks.Add(RunStartupScript);
        }

        protected override ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);

            var textureLoader = new WicTextureLoader(RenderContext);
            var audioLoader = new FFmpegAudioLoader();
            content.RegisterContentLoader(typeof(TextureAsset), textureLoader);
            content.RegisterContentLoader(typeof(AudioStream), audioLoader);

            _nitroCore.SetContent(content);
            return content;
        }

        protected override void RegisterSystems(IList<GameSystem> systems)
        {
            var inputHandler = new InputHandler(_nitroCore);
            systems.Add(inputHandler);

            var animationSystem = new AnimationSystem();
            systems.Add(animationSystem);

            var typewriterProcessor = new TypewriterAnimationProcessor();
            systems.Add(typewriterProcessor);

            var audioSystem = new AudioSystem(AudioEngine, Content);
            systems.Add(audioSystem);

            _renderSystem = new RenderSystem(RenderContext, Content);
            systems.Add(_renderSystem);
        }

        private void RunStartupScript()
        {
            var scriptLocator = new ScriptLocator(_configuration.ContentRoot);

            _nitroCore = new NitroCore(this, _configuration, Entities);
            _nssInterpreter = new NsScriptInterpreter(scriptLocator, _nitroCore);
            _nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;
            _nssInterpreter.EnteredFunction += OnEnteredFunction;

            _nssInterpreter.CreateThread(_configuration.StartupScript);
        }

        private void OnEnteredFunction(object sender, Function function)
        {
            _interpreterLog.LogCritical($"Entered function {function.Name.SimplifiedName}");
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

        public override void LoadCommonResources()
        {
            _renderSystem.LoadCommonResources();
        }

        public override void Update(float deltaMilliseconds)
        {
            MainLoopTaskScheduler.FlushQueuedTasks();
            if (!Content.IsBusy)
            {
                var elapsed = _nssInterpreter.Run(TimeSpan.MaxValue);
            }

            Systems.Update(deltaMilliseconds);
        }
    }
}
