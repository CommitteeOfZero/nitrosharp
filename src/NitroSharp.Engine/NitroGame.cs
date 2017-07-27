using NitroSharp.Foundation;
using NitroSharp.Foundation.Audio;
using NitroSharp.Foundation.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript.Execution;
using System;
using NitroSharp.NsScript;
using System.Collections.Generic;
using NitroSharp.Audio;
using System.IO;
using NitroSharp.Foundation.Animation;
using NitroSharp.Foundation.Graphics;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace NitroSharp
{
    public class NitroGame : Game
    {
        private readonly NitroConfiguration _configuration;
        private readonly string _nssFolder;

        private AudioSystem _audioSystem;
        internal RenderSystem _renderSystem;
        private InputHandler _inputHandler;

        private NsScriptInterpreter _nssInterpreter;
        private NitroCore _nitroCore;
        private Task _interpreterProc;
        private SemaphoreSlim _prepareNextFrameSignal = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private volatile bool _nextFrameReady = false;

        private ILogger _interpreterLog;
        private ILogger _entityLog;

        public NitroGame(NitroConfiguration configuration)
        {
            _configuration = configuration;
            _nssFolder = Path.Combine(configuration.ContentRoot, "nss");
            SetupLogging();
        }

        protected override void SetParameters(GameParameters parameters)
        {
            parameters.WindowWidth = _configuration.WindowWidth;
            parameters.WindowHeight = _configuration.WindowHeight;
            parameters.WindowTitle = _configuration.WindowTitle;
            parameters.EnableVSync = _configuration.EnableVSync;
        }

        protected override void RegisterStartupTasks(IList<Action> tasks)
        {
            tasks.Add(() => LoadStartupScript());
        }

        protected override ContentManager CreateContentManager()
        {
            var content = new ContentManager(_configuration.ContentRoot);

            var textureLoader = new WicTextureLoader(RenderContext);
            var audioLoader = new FFmpegAudioLoader(AudioEngine);
            content.RegisterContentLoader(typeof(Texture2D), textureLoader);
            content.RegisterContentLoader(typeof(AudioStream), audioLoader);

            _nitroCore.SetContent(content);
            return content;
        }

        protected override void RegisterSystems(IList<GameSystem> systems)
        {
            _inputHandler = new InputHandler(Window, _nitroCore);

            var animationSystem = new AnimationSystem();
            systems.Add(animationSystem);

            _audioSystem = new AudioSystem(AudioEngine);
            systems.Add(_audioSystem);

            _renderSystem = new RenderSystem(RenderContext, _configuration);
            systems.Add(_renderSystem);
        }

        private void LoadStartupScript()
        {
            _nitroCore = new NitroCore(this, _configuration, Entities);
            _nssInterpreter = new NsScriptInterpreter(_nitroCore, LocateScript);
            _nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;
            _nssInterpreter.EnteredFunction += OnEnteredFunction;

            _nssInterpreter.CreateThread("__MAIN", _configuration.StartupScript);
        }

        private Stream LocateScript(string path)
        {
            return File.OpenRead(Path.Combine(_nssFolder, path.Replace("nss/", string.Empty)));
        }

        private void OnEnteredFunction(object sender, Function function)
        {
            //_interpreterLog.LogCritical($"Entered function {function.Name.SimplifiedName}");
        }

        private void OnBuiltInCallDispatched(object sender, BuiltInFunctionCall call)
        {
            if (call.CallingThread == _nitroCore.MainThread)
            {
                //_interpreterLog.LogInformation($"Built-in call: {call.ToString()}");
            }
        }

        private void SetupLogging()
        {
            var loggerFactory = new LoggerFactory().AddConsole();
            _interpreterLog = loggerFactory.CreateLogger("Interpreter");
            //_entityLog = loggerFactory.CreateLogger("Entity System");

            //Entities.EntityRemoved += (o, e) => _entityLog.LogInformation($"Removed entity '{e.Name}'");
        }

        public override async Task OnInitialized()
        {
            var t1 = Task.Run(() => _renderSystem.PreallocateResources());
            var t2 = Task.Run(() => _audioSystem.PreallocateResources());
            await Task.WhenAll(t1, t2);

            Systems.RefreshEntityLists();
            Systems.Update(0);

            _interpreterProc = Task.Run(() => RunInterpreterLoop());
        }

        public override void Update(float deltaMilliseconds)
        {
            if (_nextFrameReady)
            {
                Systems.RefreshEntityLists();
                _inputHandler.Update(deltaMilliseconds);

                if (!_nssInterpreter.Threads.Any())
                {
                    Exit();
                }

                _nextFrameReady = false;
                _prepareNextFrameSignal.Release();
            }
            else if (_interpreterProc.IsFaulted)
            {
                throw _interpreterProc.Exception.InnerException;
            }

            var enumerator = Systems.All.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var system = enumerator.Current;
                system.Update(deltaMilliseconds);
            }

            RenderContext.Present();
        }

        private async Task RunInterpreterLoop()
        {
            while (Running && !ShutdownCancellation.IsCancellationRequested)
            {
                await _prepareNextFrameSignal.WaitAsync(ShutdownCancellation.Token).ConfigureAwait(false);
                _nssInterpreter.Run(TimeSpan.MaxValue);
                _nextFrameReady = true;
            }
        }
    }
}
