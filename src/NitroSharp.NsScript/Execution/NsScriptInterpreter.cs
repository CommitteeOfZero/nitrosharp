using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NitroSharp.NsScript.Execution
{
    public sealed class NsScriptInterpreter
    {
        private enum ThreadAction
        {
            Create,
            Terminate,
            Suspend,
            Resume
        }

        private readonly SourceFileManager _sourceFileManager;
        private readonly EngineImplementationBase _engineImplementation;
        private readonly ExpressionEvaluator _expressionEvaluator;

        private readonly Dictionary<string, ThreadContext> _threads;
        private readonly ConcurrentQueue<(ThreadContext thread, ThreadAction action, TimeSpan)> _pendingThreadActions;
        private readonly HashSet<ThreadContext> _activeThreads;
        private ThreadContext _currentThread;
        private readonly Stopwatch _timer;

        public NsScriptInterpreter(Func<SourceFileReference, Stream> sourceFileLocator, EngineImplementationBase engineImplementation)
        {
            _sourceFileManager = new SourceFileManager(sourceFileLocator);
            engineImplementation.SetInterpreter(this);
            _engineImplementation = engineImplementation;

            _threads = new Dictionary<string, ThreadContext>();
            _activeThreads = new HashSet<ThreadContext>();
            _pendingThreadActions = new ConcurrentQueue<(ThreadContext thread, ThreadAction action, TimeSpan)>();

            Globals = new MemorySpace();
            _expressionEvaluator = new ExpressionEvaluator(Globals, _engineImplementation);

            _timer = Stopwatch.StartNew();
        }

        private Frame CurrentFrame => _currentThread.CurrentFrame;

        public MemorySpace Globals { get; }
        public IEnumerable<ThreadContext> Threads => _threads.Values;

        public bool TryGetThread(string name, out ThreadContext thread) => _threads.TryGetValue(name, out thread);
        public void CreateThread(string name, string symbolName, bool start = true)
        {
            CreateThread(name, CurrentFrame.Module, symbolName, start);
        }

        public void CreateThread(string name, SourceFileReference moduleName, string symbolName, bool start = true)
        {
            var module = _sourceFileManager.Resolve(moduleName);
            CreateThread(name, module, symbolName, start);
        }

        public void CreateThread(string name, MergedSourceFileSymbol module, string symbolName, bool start = true)
        {
            Debug.WriteLine($"Creating thread '{symbolName}'");
            var member = module.LookupMember(symbolName);
            var thread = new ThreadContext(name, module, member);

            if (_threads.Count == 0)
            {
                _engineImplementation.MainThread = thread;
            }
            if (!start)
            {
                CommitSuspendThread(thread);
            }

            _pendingThreadActions.Enqueue((thread, ThreadAction.Create, TimeSpan.Zero));
        }

        public void Run(CancellationToken cancellationToken)
        {
            var time = _timer.Elapsed;
            while (_threads.Count > 0 || _pendingThreadActions.Count > 0)
            {
                ProcessPendingThreadActions();
                foreach (var thread in _threads.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (thread.IsSuspended)
                    {
                        var delta = time - thread.SuspensionTime;
                        if (delta >= thread.SleepTimeout)
                        {
                            CommitResumeThread(thread);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    _currentThread = thread;
                    Tick();

                    if (thread.DoneExecuting)
                    {
                        TerminateThread(thread);
                    }
                }

                if (_activeThreads.Count == 0)
                {
                    return;
                }
            }
        }

        private void Tick()
        {
            ExecuteStatement(_currentThread.PC);
        }

        private ConstantValue Evaluate(Expression expression) => Evaluate(expression, CurrentFrame.Arguments);
        private ConstantValue Evaluate(Expression expression, MemorySpace locals)
        {
            return _expressionEvaluator.Evaluate(expression, locals);
        }

        private void Advance()
        {
            _currentThread.Advance();
        }

        internal void PushContinuation(ImmutableArray<Statement> statements, bool advance = false)
        {
            CurrentFrame.ContinueWith(statements, advance);
        }

        internal void PushContinuation(Statement statement, bool advance = false)
        {
            CurrentFrame.ContinueWith(statement, advance);
        }

        private void ExecuteStatement(Statement statement)
        {
            switch (statement.Kind)
            {
                case SyntaxNodeKind.Block:
                    var block = (Block)statement;
                    PushContinuation(block.Statements, advance: true);
                    break;

                case SyntaxNodeKind.ExpressionStatement:
                    ExpressionStatement((ExpressionStatement)statement);
                    break;

                case SyntaxNodeKind.CallChapterStatement:
                    CallChapter((CallChapterStatement)statement);
                    break;

                case SyntaxNodeKind.IfStatement:
                    If((IfStatement)statement);
                    break;

                case SyntaxNodeKind.WhileStatement:
                    While((WhileStatement)statement);
                    break;

                case SyntaxNodeKind.BreakStatement:
                    CurrentFrame.Break();
                    break;

                case SyntaxNodeKind.ReturnStatement:
                    _currentThread.PopFrame();
                    break;

                case SyntaxNodeKind.Paragraph:
                    VisitParagraph((Paragraph)statement);
                    break;

                case SyntaxNodeKind.PXmlString:
                    var node = (PXmlString)statement;
                    _engineImplementation.CurrentThread = _currentThread;
                    _engineImplementation.DisplayDialogue(node.Text);
                    Advance();
                    break;

                case SyntaxNodeKind.PXmlLineSeparator:
                    _engineImplementation.CurrentThread = _currentThread;
                    _engineImplementation.WaitForInput();
                    Advance();
                    break;

                default:
                    Advance();
                    break;
            }
        }

        private void CallChapter(CallChapterStatement statement)
        {
            var module = _sourceFileManager.Resolve(statement.ModuleName.Value);
            var target = (ChapterSymbol)module.LookupMember("main");
            var stackFrame = new Frame(module, target);

            CurrentFrame.Advance();
            _currentThread.PushFrame(stackFrame);
        }

        private void ExpressionStatement(ExpressionStatement expressionStatement)
        {
            if (expressionStatement.Expression is FunctionCall functionCall
                && functionCall.Target.Symbol is FunctionSymbol target)
            {
                PrepareFunctionCall(functionCall, target);
                return;
            }

            _engineImplementation.CurrentThread = _currentThread;
            var locals = CurrentFrame.Arguments;
            Advance();
            Evaluate(expressionStatement.Expression, locals);
        }

        private void PrepareFunctionCall(FunctionCall functionCall, FunctionSymbol target)
        {
            var stackFrame = new Frame(CurrentFrame.Module, target);

            Debug.WriteLine($"Entering function '{target.Name}'");

            var declaration = (Function)target.Declaration;
            var parameters = declaration.Parameters;
            var arguments = functionCall.Arguments;

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                bool argSpecified = i < arguments.Length;

                ConstantValue value = argSpecified ? Evaluate(functionCall.Arguments[i]) : ConstantValue.Null;
                stackFrame.SetArgument(parameter.Name.Value, value);

                if (parameter.Name.Sigil == SigilKind.Dollar)
                {
                    Globals.Set(parameter.Name.Value, value);
                }
            }

            CurrentFrame.Advance();
            _currentThread.PushFrame(stackFrame);
        }

        private void If(IfStatement ifStatement)
        {
            var condition = Evaluate(ifStatement.Condition);
            if (condition)
            {
                PushContinuation(ifStatement.IfTrueStatement, advance: true);
            }
            else if (ifStatement.IfFalseStatement != null)
            {
                PushContinuation(ifStatement.IfFalseStatement, advance: true);
            }
            else
            {
                Advance();
            }
        }

        private void While(WhileStatement whileStatement)
        {
            if (Evaluate(whileStatement.Condition))
            {
                PushContinuation(whileStatement.Body);
            }
            else
            {
                Advance();
            }
        }

        private void VisitParagraph(Paragraph paragraph)
        {
            Globals.Set("SYSTEM_present_preprocess", ConstantValue.Create(paragraph.AssociatedBox));
            Globals.Set("SYSTEM_present_text", ConstantValue.Create(paragraph.Identifier));

            _engineImplementation.NotifyParagraphEntered(paragraph);
            Advance();
        }

        private void ProcessPendingThreadActions()
        {
            while (_pendingThreadActions.TryDequeue(out var tuple))
            {
                (ThreadContext thread, ThreadAction action, TimeSpan suspensionTimeout) = tuple;
                switch (tuple.action)
                {
                    case ThreadAction.Create:
                        _threads[thread.Name] = thread;
                        if (!thread.IsSuspended)
                        {
                            _activeThreads.Add(thread);
                        }
                        break;

                    case ThreadAction.Terminate:
                        CommitTerminateThread(thread);
                        break;

                    case ThreadAction.Suspend:
                        CommitSuspendThread(thread, suspensionTimeout);
                        break;

                    case ThreadAction.Resume:
                        CommitResumeThread(thread);
                        break;
                }

            }
        }

        public void ResumeThread(ThreadContext thread)
        {
            _pendingThreadActions.Enqueue((thread, ThreadAction.Resume, TimeSpan.Zero));
        }

        public void SuspendThread(ThreadContext thread) => SuspendThread(thread, TimeSpan.MaxValue);
        public void SuspendThread(ThreadContext thread, TimeSpan timeout)
        {
            _pendingThreadActions.Enqueue((thread, ThreadAction.Suspend, timeout));
        }

        public void TerminateThread(ThreadContext thread)
        {
            _pendingThreadActions.Enqueue((thread, ThreadAction.Terminate, TimeSpan.Zero));
        }


        private void CommitSuspendThread(ThreadContext thread) => CommitSuspendThread(thread, TimeSpan.MaxValue);
        private void CommitSuspendThread(ThreadContext thread, TimeSpan timeout)
        {
            thread.SuspensionTime = _timer.Elapsed;
            thread.SleepTimeout = timeout;
            thread.IsSuspended = true;

            _activeThreads.Remove(thread);
            Debug.WriteLine($"Suspending {thread.Name} ({thread.EntryPoint.Name})");
        }

        private void CommitResumeThread(ThreadContext thread)
        {
            if (_threads.ContainsKey(thread.Name))
            {
                thread.IsSuspended = false;
                thread.SuspensionTime = TimeSpan.Zero;
                thread.SleepTimeout = TimeSpan.Zero;

                _activeThreads.Add(thread);
                Debug.WriteLine($"Resuming {thread.Name} ({thread.EntryPoint.Name})");
            }
            else
            {
                Debug.WriteLine("Resuming a dead thread");
            }
        }

        private void CommitTerminateThread(ThreadContext thread)
        {
            _threads.Remove(thread.Name);
            _activeThreads.Remove(thread);

            Debug.WriteLine($"Terminating {thread.Name} ({thread.EntryPoint.Name})");
        }
    }
}
