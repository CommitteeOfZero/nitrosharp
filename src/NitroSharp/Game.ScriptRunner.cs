using NitroSharp.Animation;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace NitroSharp
{
    public partial class Game
    {
        internal sealed class ScriptRunner : Actor
        {
            public enum Status
            {
                Running,
                AwaitingPresenterState,
                NewStateReady,
                Crashed
            }

            private readonly Configuration _configuration;
            private readonly Logger _logger;
            private readonly bool _usingDedicatedThread;
            private readonly CancellationTokenSource _shutdownCancellation;
            private readonly string _nssFolder;
            private readonly string _bytecodeCacheDir;
            private VirtualMachine? _nssInterpreter;
            private readonly NsBuiltins _builtinFunctions;

            private volatile Status _status;
            private Task? _interpreterProc;

            public Exception? LastException { get; private set; }

            public ScriptRunner(Game game, World world) : base(world)
            {
                _configuration = game._configuration;
                _logger = game._logger;
                _usingDedicatedThread = _configuration.UseDedicatedInterpreterThread;
                _nssFolder = Path.Combine(_configuration.ContentRoot, "nss");
                _bytecodeCacheDir = _nssFolder.Replace("nss", "nsx");
                _builtinFunctions = new NsBuiltins(game);
                _builtinFunctions.SetWorld(world);
                
                _shutdownCancellation = new CancellationTokenSource();
            }

            public Status Tick()
            {
                return _usingDedicatedThread ? _status : Run();
            }

            public void Resume()
            {
                _status = Status.Running;
            }

            public void LoadStartupScript()
            {
                const string globalsFileName = "_globals";
                string globalsPath = Path.Combine(_bytecodeCacheDir, globalsFileName);
                if (!File.Exists(globalsPath) || !ValidateBytecodeCache())
                {
                    _logger.LogInformation("Bytecode cache is not up-to-date. Recompiling the scripts...");
                    var compilation = new Compilation(_nssFolder, _bytecodeCacheDir, globalsFileName);
                    compilation.Emit(compilation.GetSourceModule(_configuration.StartupScript));
                }
                else
                {
                    _logger.LogInformation("Bytecode cache is up-to-date.");
                }

                var nsxLocator = new FileSystemNsxModuleLocator(_bytecodeCacheDir);
                _nssInterpreter = new VirtualMachine(nsxLocator, File.OpenRead(globalsPath), _builtinFunctions);
                _nssInterpreter.CreateThread(
                    "__MAIN",
                    _configuration.StartupScript.Replace(".nss", string.Empty), "main");
            }

            private bool ValidateBytecodeCache()
            {
                string startupScript = _configuration.StartupScript.Replace('\\', '/');
                startupScript = startupScript.Remove(startupScript.Length - 4);
                foreach (string nssFile in Directory.EnumerateFiles(_nssFolder, "*.nss", SearchOption.AllDirectories))
                {
                    string nsxFile = nssFile.Replace("nss", "nsx");
                    try
                    {
                        using (FileStream nsxStream = File.OpenRead(nsxFile))
                        {
                            long nsxTimestamp = NsxModule.GetSourceModificationTime(nsxStream);
                            long nssTimestamp = new DateTimeOffset(File.GetLastWriteTimeUtc(nssFile))
                                .ToUnixTimeSeconds();
                            if (nsxTimestamp != nssTimestamp)
                            {
                                return false;
                            }
                        }

                    }
                    catch (FileNotFoundException)
                    {
                        string nsxRelativePath = Path.GetRelativePath(relativeTo: _bytecodeCacheDir, nsxFile)
                            .Replace('\\', '/');
                        nsxRelativePath = nsxRelativePath.Remove(nsxRelativePath.Length - 4);
                        if (nsxRelativePath.Equals(startupScript))
                        {
                            return false;
                        }

                        continue;
                    }
                }

                return true;
            }

            public void StartInterpreter()
            {
                if (_usingDedicatedThread)
                {
                    _interpreterProc = Task.Factory.StartNew(
                        (Action)InterpreterLoop, TaskCreationOptions.LongRunning);
                }
            }

            private void InterpreterLoop()
            {
                while (!_shutdownCancellation.IsCancellationRequested)
                {
                    while (_status != Status.Running)
                    {
                        Thread.Sleep(1);
                    }

                    try
                    {
                        _status = Run();
                    }
                    catch (Exception ex)
                    {
                        _status = Status.Crashed;
                        LastException = ex;
                    }
                }
            }

            private Status Run()
            {
                Debug.Assert(_nssInterpreter != null);
                bool threadStateChanged = _nssInterpreter.RefreshThreadState();
                if (threadStateChanged || _nssInterpreter.ProcessPendingThreadActions())
                {
                    return Status.AwaitingPresenterState;
                }

                bool ranAnyCode = _nssInterpreter.Run(CancellationToken.None);
                if (ranAnyCode)
                {
                    Queue<Message> messagesForPresenter = _builtinFunctions.MessagesForPresenter;
                    while (messagesForPresenter.Count > 0)
                    {
                        Message message = messagesForPresenter.Dequeue();
                        PostMessage(message);
                    }
                }

                return ranAnyCode ? Status.NewStateReady : Status.AwaitingPresenterState;
            }

            protected override void HandleMessages(Queue<Message> messages)
            {
                Debug.Assert(_nssInterpreter != null);
                foreach (Message message in messages)
                {
                    switch (message.Kind)
                    {
                        case MessageKind.SuspendMainThread:
                            _nssInterpreter.SuspendMainThread();
                            break;
                        case MessageKind.ResumeMainThread:
                            _nssInterpreter.ResumeMainThread();
                            break;
                        case MessageKind.ThreadAction:
                            RunThreadAction((ThreadActionMessage)message);
                            break;
                        case MessageKind.AnimationCompleted:
                            var animCompletedMsg = (AnimationCompletedMessage)message;
                            PropertyAnimation animation = animCompletedMsg.Animation;
                            ThreadContext thread = animation.WaitingThread;
                            if (thread != null)
                            {
                                _nssInterpreter.ResumeThread(thread);
                            }
                            break;
                        case MessageKind.ChoiceSelected:
                            var choiceSelectedMsg = (ChoiceSelectedMessage)message;
                            _builtinFunctions.SelectedChoice = choiceSelectedMsg.ChoiceName;
                            break;
                    }
                }
            }

            private void RunThreadAction(ThreadActionMessage message)
            {
                Debug.Assert(_nssInterpreter != null);
                InterpreterThreadInfo threadInfo = message.ThreadInfo;
                switch (message.Action)
                {
                    case ThreadActionMessage.ActionKind.StartOrResume:
                        if (_nssInterpreter.TryGetThread(threadInfo.Name, out ThreadContext thread))
                        {
                            _nssInterpreter.ResumeThread(thread);
                        }
                        else
                        {
                            _nssInterpreter.CreateThread(
                                threadInfo.Name, threadInfo.Module, threadInfo.Target, start: true);
                        }
                        break;

                    case ThreadActionMessage.ActionKind.Terminate:
                        if (_nssInterpreter.TryGetThread(threadInfo.Name, out thread))
                        {
                            _nssInterpreter.TerminateThread(thread);
                        }
                        break;
                }
            }
        }
    }
}
