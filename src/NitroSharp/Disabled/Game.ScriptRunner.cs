using NitroSharp.New;
using NitroSharp.NsScript.Compiler;
using NitroSharp.NsScript.VM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

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
            private readonly string _nssFolder;
            private readonly string _bytecodeCacheDir;
            private NsScriptVM? _vm;
            private readonly Builtins _builtinFunctions;

            public Exception? LastException { get; private set; }

            public ScriptRunner(Game game, World world) : base(world)
            {
                _configuration = game._configuration;
                _logger = game._logger;
                _nssFolder = Path.Combine(_configuration.ContentRoot, "nss");
                _bytecodeCacheDir = _nssFolder.Replace("nss", "nsx");
                _builtinFunctions = new Builtins(game, world);
            }

            public Status Tick()
            {
                Debug.Assert(_vm != null);
                bool threadStateChanged = _vm.RefreshThreadState();
                if (threadStateChanged || _vm.ProcessPendingThreadActions())
                {
                    return Status.AwaitingPresenterState;
                }

                bool ranAnyCode = _vm.Run(CancellationToken.None);
                if (ranAnyCode)
                {
                    //Queue<Message> messagesForPresenter = _builtinFunctions.MessagesForPresenter;
                    //while (messagesForPresenter.Count > 0)
                    //{
                    //    Message message = messagesForPresenter.Dequeue();
                    //    PostMessage(message);
                    //}
                }

                return ranAnyCode ? Status.NewStateReady : Status.AwaitingPresenterState;
            }

            public void LoadStartupScript()
            {
                const string globalsFileName = "_globals";
                string globalsPath = Path.Combine(_bytecodeCacheDir, globalsFileName);
                if (_configuration.SkipUpToDateCheck || !File.Exists(globalsPath) || !ValidateBytecodeCache())
                {
                    if (!Directory.Exists(_bytecodeCacheDir))
                    {
                        Directory.CreateDirectory(_bytecodeCacheDir);
                        _logger.LogInformation("Bytecode cache is empty. Compiling the scripts...");
                    }
                    else
                    {
                        _logger.LogInformation("Bytecode cache is not up-to-date. Recompiling the scripts...");
                        foreach (string file in Directory.EnumerateFiles(
                            _bytecodeCacheDir, "*.nsx", SearchOption.AllDirectories))
                        {
                            File.Delete(file);
                        }
                    }

                    Encoding? sourceEncoding = _configuration.UseUtf8 ? Encoding.UTF8 : null;
                    var compilation = new Compilation(_nssFolder, _bytecodeCacheDir, globalsFileName, sourceEncoding);
                    compilation.Emit(compilation.GetSourceModule(_configuration.StartupScript));
                }
                else
                {
                    _logger.LogInformation("Bytecode cache is up-to-date.");
                }

                var nsxLocator = new FileSystemNsxModuleLocator(_bytecodeCacheDir);
                _vm = new NsScriptVM(nsxLocator, File.OpenRead(globalsPath), _builtinFunctions);
                _vm.CreateThread(
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
                    catch
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

            protected override void HandleMessages(Queue<Message> messages)
            {
                Debug.Assert(_vm != null);
                foreach (Message message in messages)
                {
                    switch (message.Kind)
                    {
                        case MessageKind.SuspendMainThread:
                            _vm.SuspendMainThread();
                            break;
                        case MessageKind.ResumeMainThread:
                            _vm.ResumeMainThread();
                            break;
                        case MessageKind.ThreadAction:
                            RunThreadAction((ThreadActionMessage)message);
                            break;
                        case MessageKind.AnimationCompleted:
                            //var animCompletedMsg = (AnimationCompletedMessage)message;
                            //PropertyAnimation animation = animCompletedMsg.Animation;
                            //ThreadContext thread = animation.WaitingThread;
                            //if (thread != null)
                            //{
                            //    _vm.ResumeThread(thread);
                            //}
                            break;
                    }
                }
            }

            private void RunThreadAction(ThreadActionMessage message)
            {
                Debug.Assert(_vm != null);
                InterpreterThreadInfo threadInfo = message.ThreadInfo;
                switch (message.Action)
                {
                    case ThreadActionMessage.ActionKind.StartOrResume:
                        if (_vm.TryGetThread(threadInfo.Name, out ThreadContext? thread))
                        {
                            _vm.ResumeThread(thread);
                        }
                        else
                        {
                            _vm.CreateThread(
                                threadInfo.Name, threadInfo.Module, threadInfo.Target, start: true);
                        }
                        break;

                    case ThreadActionMessage.ActionKind.Terminate:
                        if (_vm.TryGetThread(threadInfo.Name, out thread))
                        {
                            _vm.TerminateThread(thread);
                        }
                        break;
                    case ThreadActionMessage.ActionKind.Suspend:
                        if (_vm.TryGetThread(threadInfo.Name, out thread))
                        {
                            _vm.SuspendThread(thread);
                        }
                        break;
                }
            }
        }
    }
}
