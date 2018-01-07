using NitroSharp.Foundation;
using NitroSharp.Foundation.Audio;
using NitroSharp.Foundation.Content;
using NitroSharp.Graphics;
using System;
using NitroSharp.NsScript;
using System.Collections.Generic;
using NitroSharp.Audio;
using System.IO;
using NitroSharp.Foundation.Animation;
using NitroSharp.Foundation.Graphics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Serilog;
using Serilog.Events;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Execution;

namespace NitroSharp
{
    public class NitroGame : Game
    {
        private readonly NitroConfiguration _configuration;
        private readonly string _nssFolder;

        private AudioSystem _audioSystem;
        // TODO: should be private.
        internal RenderSystem _renderSystem;
        private InputHandler _inputHandler;

        private NsScriptInterpreter _nssInterpreter;
        private NitroCore _nitroCore;
        private Task _interpreterProc;
        private volatile bool _nextStateReady = false;

        private ILogger _log;
        private string _logPath;

        private readonly Stopwatch _perfCounter = new Stopwatch();
        private PerfStats _perfStats;

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

            _perfStats = new PerfStats(systems.Count);
        }

        private void LoadStartupScript()
        {
            _nitroCore = new NitroCore(this, Entities);
            _nssInterpreter = new NsScriptInterpreter(LocateScript, _nitroCore);
            //_nssInterpreter.BuiltInCallScheduled += OnBuiltInCallDispatched;
            //_nssInterpreter.EnteredFunction += OnEnteredFunction;

            _nssInterpreter.CreateThread("__MAIN", _configuration.StartupScript, "main");
        }

        private Stream LocateScript(SourceFileReference fileRef)
        {
            return File.OpenRead(Path.Combine(_nssFolder, fileRef.FilePath.Replace("nss/", string.Empty)));
        }

        public override async Task OnInitialized()
        {
            var t1 = Task.Run(() => _renderSystem.PreallocateResources());
            var t2 = Task.Run(() => _audioSystem.PreallocateResources());
            await Task.WhenAll(t1, t2);

            Systems.ProcessEntityUpdates();
            Systems.Update(0);

            _interpreterProc = Task.Factory.StartNew(() => RunInterpreterLoop(), TaskCreationOptions.LongRunning);
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
#if RELEASE
            catch (Exception e)
            {
                _log.Fatal(e, string.Empty);

                string message = $"An error has occurred:\n{e.Message}\n\nYou can get more information by examining the file '{_logPath}'.";
                MessageBox.Show(Window.Handle, message, isError: true);
                Exit();
            }
#endif
        }

        private void UpdateCore(float deltaMilliseconds)
        {
            double elapsed = 0.0d;
            _perfStats.Clear();
            _perfCounter.Restart();

            if (_nextStateReady)
            {
                Systems.ProcessEntityUpdates();
                _inputHandler.Update(deltaMilliseconds);

                if (!_nssInterpreter.Threads.Any())
                {
                    Exit();
                }

                _nextStateReady = false;

                elapsed = _perfCounter.Elapsed.TotalMilliseconds;
                _perfStats.ProcessingEntityUpdates = elapsed - _perfStats.Total;
                _perfStats.Total = elapsed;
            }
            else if (_interpreterProc.IsFaulted)
            {
                throw _interpreterProc.Exception.InnerException;
            }

            var enumerator = Systems.All.GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext())
            {
                var system = enumerator.Current;
                system.Update(deltaMilliseconds);

                elapsed = _perfCounter.Elapsed.TotalMilliseconds;
                _perfStats.SystemUpdateTimes[i] = elapsed - _perfStats.Total;
                _perfStats.Total = elapsed;
                i++;
            }

            RenderContext.Present();

            _perfCounter.Stop();
            elapsed = _perfCounter.Elapsed.TotalMilliseconds;
            _perfStats.FlipTime = elapsed - _perfStats.Total;
            _perfStats.Total = elapsed;

            if (_configuration.EnableDiagnostics && _perfStats.Total > 20)
            {
                LogPerfStats();
            }
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

        private void SetupLogging()
        {
            string logFileName = $"log_{DateTime.Now.ToString(@"yyyymmdd_HHmm_ss")}.txt";
            _logPath = Path.Combine(Path.GetTempPath(), _configuration.ProductName, logFileName);

            var fileMinLevel = _configuration.EnableDiagnostics ? LogEventLevel.Debug : LogEventLevel.Error;
            var flushInterval = TimeSpan.FromSeconds(2);

            var loggerFactory = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Async(x => x.File(_logPath, fileMinLevel, buffered: true, flushToDiskInterval: flushInterval))
                .WriteTo.ColoredConsole(LogEventLevel.Debug);

            _log = loggerFactory.CreateLogger();

            //Entities.EntityRemoved += (o, e) => _entityLog.LogInformation($"Removed entity '{e.Name}'");
        }

        private void OnEnteredFunction(object sender, Function function)
        {
            _log.Debug("Entered function " + function.Name.Value);
        }

        //private void OnBuiltInCallDispatched(object sender, BuiltInFunctionCall call)
        //{
        //    if (call.CallingThread == _nitroCore.MainThread)
        //    {
        //        _log.Debug("Built-in call: " + call.ToString());
        //    }
        //}

        private void LogPerfStats()
        {
            double animationSystemTime = _perfStats.SystemUpdateTimes[0];
            double audioSystemTime = _perfStats.SystemUpdateTimes[1];
            double renderSystemTime = _perfStats.SystemUpdateTimes[2];

            const int decimals = 3;
            var warning = new StringBuilder("Update took longer than expected.");
            warning.AppendLine();
            warning.Append("\tTotal: ");
            warning.Append(Math.Round(_perfStats.Total, decimals));
            warning.Append("ms");
            warning.AppendLine();

            warning.Append("\tProcessing entity updates: ");
            warning.Append(Math.Round(_perfStats.ProcessingEntityUpdates, decimals));
            warning.Append("ms");
            warning.AppendLine();

            warning.Append("\tAnimationSystem time: ");
            warning.Append(Math.Round(animationSystemTime, decimals));
            warning.Append("ms");
            warning.AppendLine();

            warning.Append("\tAudioSystem time: ");
            warning.Append(Math.Round(audioSystemTime, decimals));
            warning.Append("ms");
            warning.AppendLine();

            warning.Append("\tRenderSystem time: ");
            warning.Append(Math.Round(renderSystemTime, decimals));
            warning.Append("ms");
            warning.AppendLine();

            warning.Append("\tFlip time: ");
            warning.Append(Math.Round(_perfStats.FlipTime, decimals));
            warning.Append("ms");
            warning.AppendLine();

            _log.Warning(warning.ToString());
        }

        private sealed class PerfStats
        {
            public PerfStats(int systemCount)
            {
                SystemUpdateTimes = new double[systemCount];
            }

            public double Total;
            public double ProcessingEntityUpdates;
            public double FlipTime;
            public readonly double[] SystemUpdateTimes;

            public void Clear()
            {
                Total = 0;
                ProcessingEntityUpdates = 0;

                // Don't really need to reset the other fields.
            }
        }
    }
}
