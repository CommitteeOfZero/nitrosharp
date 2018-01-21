using NitroSharp.NsScript.IR;
using NitroSharp.NsScript.Symbols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NitroSharp.NsScript.Execution
{
    public class NsScriptInterpreter
    {
        private enum ThreadAction
        {
            Create,
            Terminate,
            Suspend,
            Resume
        }

        private readonly SourceFileManager _sourceFileManager;
        private readonly EngineImplementation _engineImplementation;

        private readonly Dictionary<string, ThreadContext> _threads;
        private readonly ConcurrentQueue<(ThreadContext thread, ThreadAction action, TimeSpan)> _pendingThreadActions;
        private readonly HashSet<ThreadContext> _activeThreads;
        
        private readonly Environment _globals;
        private readonly Stopwatch _timer;
        private readonly IRBuilder _irBuilder;

        public NsScriptInterpreter(Func<SourceFileReference, Stream> sourceFileLocator, EngineImplementation engineImplementation)
        {
            _sourceFileManager = new SourceFileManager(sourceFileLocator);
            engineImplementation.SetInterpreter(this);
            _engineImplementation = engineImplementation;

            _threads = new Dictionary<string, ThreadContext>();
            _activeThreads = new HashSet<ThreadContext>();
            _pendingThreadActions = new ConcurrentQueue<(ThreadContext thread, ThreadAction action, TimeSpan)>();

            _globals = new Environment();
            _timer = Stopwatch.StartNew();
            _irBuilder = new IRBuilder();
        }
        
        public IEnumerable<ThreadContext> Threads => _threads.Values;
        internal ThreadContext CurrentThread;
        private Frame CurrentFrame => CurrentThread.CurrentFrame;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ConstantValue PopValue() => CurrentThread.Stack.Pop();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushValue(ConstantValue value) => CurrentThread.Stack.Push(value);
        
        private void EnsureHasLinearRepresentation(InvocableSymbol invocable)
        {
            if (invocable.LinearRepresentation == null)
            {
                invocable.LinearRepresentation = _irBuilder.Build(invocable);
            }
        }

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

        private void CreateThread(string name, MergedSourceFileSymbol module, string symbolName, bool start = true)
        {
            Debug.WriteLine($"Creating thread '{symbolName}'");
            var member = module.LookupMember(symbolName);
            EnsureHasLinearRepresentation(member);

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
            while (_threads.Count > 0 || _pendingThreadActions.Count > 0)
            {
                ProcessPendingThreadActions();
                var time = TimeSpan.MaxValue;
                foreach (var thread in _threads.Values)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (thread.IsSuspended && !thread.SuspensionTime.Equals(TimeSpan.MaxValue))
                    {
                        if (time == TimeSpan.MaxValue)
                        {
                            time = _timer.Elapsed;
                        }
                        
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

                    CurrentThread = thread;
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
            while (true)
            {
                var frame = CurrentFrame;
                ref var instruction = ref frame.FetchInstruction();
                bool advance = Execute(ref instruction);
                if (advance)
                {
                    frame.Advance();
                }

                if (CanAffectThreadState(instruction.Opcode))
                {
                    break;
                }
            }
        }

        private static bool CanAffectThreadState(Opcode opcode)
        {
            switch (opcode)
            {
                case Opcode.Call:
                case Opcode.CallFar:
                case Opcode.Say:
                case Opcode.WaitForInput:
                    return true;
                    
                default:
                    return false;
            }
        }

        private bool Execute(ref Instruction instruction)
        {            
            switch (instruction.Opcode)
            {
                case Opcode.PushValue:
                    PushValue(ref instruction);
                    break;

                case Opcode.PushGlobal:
                    PushGlobal(ref instruction);
                    break;

                case Opcode.PushLocal:
                    PushLocal(ref instruction);
                    break;

                case Opcode.ApplyBinary:
                    ApplyBinary(ref instruction);
                    break;

                case Opcode.ApplyUnary:
                    ApplyUnary(ref instruction);
                    break;

                case Opcode.AssignGlobal:
                    Assign(ref instruction, _globals);
                    break;

                case Opcode.AssignLocal:
                    Assign(ref instruction, CurrentFrame.Arguments);
                    break;

                case Opcode.ConvertToDelta:
                    ConvertToDelta();
                    break;

                case Opcode.SetDialogueBlock:
                    SetDialogueBlock(ref instruction);
                    break;

                case Opcode.Say:
                    Say(ref instruction);
                    break;

                case Opcode.WaitForInput:
                    WaitForInput();
                    break;
                    
                case Opcode.Call:
                    Call(ref instruction);
                    break;

                case Opcode.Jump:
                    Jump(ref instruction);
                    return false;

                case Opcode.JumpIfEquals:
                    return !JumpIfEquals(ref instruction);

                case Opcode.Return:
                    CurrentThread.PopFrame();
                    return false;
            }

            return true;
        }

        private void PushValue(ref Instruction instruction)
        {
            var value = (ConstantValue)instruction.Operand1;
            PushValue(value);
        }
        
        private void PushGlobal(ref Instruction instruction)
        {
            var name = (string)instruction.Operand1;
            PushValue(_globals.Get(name));
        }
        
        private void PushLocal(ref Instruction instruction)
        {
            var name = (string)instruction.Operand1;
            var value = CurrentFrame.Arguments.Get(name);
            PushValue(value);
        }
        
        private void ApplyBinary(ref Instruction instruction)
        {
            var op = (BinaryOperatorKind)instruction.Operand1;
            var left = PopValue();
            var right = PopValue();
            var result = Operator.ApplyBinary(left, op, right);

            PushValue(result);
        }
        
        private void ApplyUnary(ref Instruction instruction)
        {
            var op = (UnaryOperatorKind)instruction.Operand1;
            var operand = PopValue();
            var result = Operator.ApplyUnary(operand, op);

            PushValue(result);
        }

        private void Assign(ref Instruction instruction, Environment env)
        {
            var name = (string)instruction.Operand1;
            var op = (AssignmentOperatorKind)instruction.Operand2;
            var value = PopValue();

            var result = Operator.Assign(env, name, value, op);
            env.Set(name, result);
        }
        
        private void ConvertToDelta()
        {
            var delta = ConstantValue.Create(PopValue().DoubleValue, isDeltaValue: true);
            PushValue(delta);
        }
        
        private void SetDialogueBlock(ref Instruction instruction)
        {
            var block = (DialogueBlockSymbol)instruction.Operand1;
            EnsureHasLinearRepresentation(block);

            _globals.Set("SYSTEM_present_preprocess", ConstantValue.Create(block.AssociatedBox));
            _globals.Set("SYSTEM_present_text", ConstantValue.Create(block.Identifier));

            _engineImplementation.NotifyDialogueBlockEntered(block);
        }
        
        private void Say(ref Instruction instruction)
        {
            var text = (string)instruction.Operand1;
            _engineImplementation.DisplayDialogue(text);
        }
        
        private void WaitForInput()
        {
            _engineImplementation.WaitForInput();
        }
        
        private bool JumpIfEquals(ref Instruction instruction)
        {
            var value = (ConstantValue)instruction.Operand1;
            int targetInstrIndex = (int)instruction.Operand2;
            bool branchTaken = (PopValue() == value).BooleanValue;
            if (branchTaken)
            {
                CurrentFrame.Jump(targetInstrIndex);
            }

            return branchTaken;
        }

        private void Jump(ref Instruction instruction)
        {
            int targetInstrIndex = (int)instruction.Operand1;
            CurrentFrame.Jump(targetInstrIndex);
        }
        
        private void Call(ref Instruction instruction)
        {
            var target = (Symbol)instruction.Operand1;
            switch (target)
            {
                case FunctionSymbol function:
                    CallFunction(function);
                    break;

                case BuiltInFunctionSymbol builtInFunction:
                    CallBuiltInFunction(builtInFunction);
                    break;

                case null:
                    PushValue(ConstantValue.Null);
                    break;
            }
        }

        private void CallFunction(FunctionSymbol function)
        {
            EnsureHasLinearRepresentation(function);

            var stackFrame = new Frame(CurrentFrame.Module, function);
            var declaration = (Syntax.Function)function.Declaration;
            var parameters = declaration.Parameters;
            foreach (var parameter in parameters)
            {
                var argument = PopValue();
                stackFrame.SetArgument(parameter.Identifier.Name, argument);

                if (parameter.Identifier.IsGlobalVariable)
                {
                    _globals.Set(parameter.Identifier.Name, argument);
                }
            }

            CurrentThread.PushFrame(stackFrame);
        }
        
        private void CallBuiltInFunction(BuiltInFunctionSymbol function)
        {
            var returnValue = function.Implementation.Invoke(_engineImplementation, CurrentThread.Stack);
            if (!ReferenceEquals(returnValue, null))
            {
                PushValue(returnValue);
            }
        }

        private void ProcessPendingThreadActions()
        {
            while (_pendingThreadActions.TryDequeue(out var tuple))
            {
                (ThreadContext thread, ThreadAction action, TimeSpan suspensionTimeout) = tuple;
                switch (action)
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
            Debug.Assert(_threads.ContainsKey(thread.Name), "Attempt to resume a thread that's already been terminated.");
            
            thread.IsSuspended = false;
            thread.SuspensionTime = TimeSpan.Zero;
            thread.SleepTimeout = TimeSpan.Zero;

            _activeThreads.Add(thread);
            Debug.WriteLine($"Resuming {thread.Name} ({thread.EntryPoint.Name})");
        }

        private void CommitTerminateThread(ThreadContext thread)
        {
            _threads.Remove(thread.Name);
            _activeThreads.Remove(thread);

            Debug.WriteLine($"Terminating {thread.Name} ({thread.EntryPoint.Name})");
        }
    }
}
