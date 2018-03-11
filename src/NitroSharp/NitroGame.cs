using NitroSharp.Graphics;
using System;
using NitroSharp.NsScript;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Execution;
using NitroSharp.Content;
using NitroSharp.Animation;

namespace NitroSharp
{
    public sealed class NewNitroGame : Game
    {
        private readonly NewNitroConfiguration _configuration;
        private readonly string _nssFolder;

        private RenderSystem _renderSystem;
        private InputSystem _inputHandler;

        private NsScriptInterpreter _nssInterpreter;
        private CoreLogic _nitroCore;
        private Task _interpreterProc;
        private volatile bool _nextStateReady = false;

        public NewNitroGame(NewNitroConfiguration configuration)
        {
            _configuration = configuration;
            _nssFolder = Path.Combine(configuration.ContentRoot, "nss");
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

            var textureLoader = new WicTextureLoader(GraphicsDevice);
            content.RegisterContentLoader(typeof(BindableTexture), textureLoader);

            _nitroCore.SetContent(content);
            return content;
        }

        protected override void RegisterSystems(IList<GameSystem> systems)
        {
            _inputHandler = new InputSystem(Window, _nitroCore);

            var animationSystem = new AnimationSystem();
            systems.Add(animationSystem);

            _renderSystem = new RenderSystem(GraphicsDevice, _configuration);
            systems.Add(_renderSystem);
        }

        private void LoadStartupScript()
        {
            _nitroCore = new CoreLogic(this, Entities);
            _nssInterpreter = new NsScriptInterpreter(LocateScript, _nitroCore);
            //_nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;
            //_nssInterpreter.EnteredFunction += OnEnteredFunction;

            _nssInterpreter.CreateThread("__MAIN", _configuration.StartupScript, "main");
        }

        private Stream LocateScript(SourceFileReference fileRef)
        {
            return File.OpenRead(Path.Combine(_nssFolder, fileRef.FilePath.Replace("nss/", string.Empty)));
        }

        public override Task OnInitialized()
        {
            Systems.ProcessEntityUpdates();
            Systems.Update(0);

            _interpreterProc = Task.Factory.StartNew(() => RunInterpreterLoop(), TaskCreationOptions.LongRunning);
            return Task.FromResult(0);
        }

        public override void Update(float deltaMilliseconds)
        {
            try
            {
                UpdateCore(deltaMilliseconds);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void UpdateCore(float deltaMilliseconds)
        {
            if (_nextStateReady)
            {
                Systems.ProcessEntityUpdates();
                _inputHandler.Update(deltaMilliseconds);

                if (!_nssInterpreter.Threads.Any())
                {
                    Exit();
                }

                _nextStateReady = false;
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

            _renderSystem.Present();
        }

        private void RunInterpreterLoop()
        {
            while (Running)
            {
                while (_nextStateReady)
                {
                    Thread.Sleep(5);
                }

                _nssInterpreter.Run(CancellationToken.None);
                _nextStateReady = true;
            }
        }
    }
}
