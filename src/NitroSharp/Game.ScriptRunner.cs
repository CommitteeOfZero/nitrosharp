using NitroSharp.Animation;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                NewStateReady
            }

            private readonly Configuration _configuration;
            private readonly bool _usingDedicatedThread;
            private readonly CancellationTokenSource _shutdownCancellation;
            private readonly string _nssFolder;
            private VirtualMachine _nssInterpreter;
            private readonly NsBuiltins _builtinFunctions;

            private volatile Status _status;
            private Task _interpreterProc;

            public ScriptRunner(Game game, World world) : base(world)
            {
                _configuration = game._configuration;
                _usingDedicatedThread = _configuration.UseDedicatedInterpreterThread;
                _nssFolder = Path.Combine(_configuration.ContentRoot, "nss");
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
                var compilation = new Compilation(new DefaultSourceReferenceResolver(_nssFolder));
                compilation.Emit(compilation.GetSourceModule(_configuration.StartupScript));

                _nssInterpreter = new VirtualMachine(new FileSystemNsxModuleLocator(_nssFolder.Replace("nss", "nsx")), File.OpenRead("S:/globals"), _builtinFunctions);

                ThreadContext mainThread = _nssInterpreter
                    .CreateThread("__MAIN", _configuration.StartupScript.Replace(".nss", string.Empty), "main");
            }

            public void StartInterpreter()
            {
                if (_usingDedicatedThread)
                {
                    _interpreterProc = Task.Factory.StartNew((Action)InterpreterLoop, TaskCreationOptions.LongRunning);
                }
            }

            private void InterpreterLoop()
            {
                while (!_shutdownCancellation.IsCancellationRequested)
                {
                    while (_status != Status.Running)
                    {
                        Thread.Sleep(5);
                    }

                    _status = Run();
                }
            }

            private Status Run()
            {
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

            protected override void HandleMessages<T>(T messages)
            {
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
