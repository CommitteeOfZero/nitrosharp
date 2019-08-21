using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    public sealed class VirtualMachine
    {
        private readonly struct ThreadAction
        {
            public enum ActionKind
            {
                Create,
                Terminate,
                Suspend,
                Resume
            }

            public readonly ThreadContext Thread;
            public readonly ActionKind Kind;
            public readonly TimeSpan? Timeout;

            public static ThreadAction Create(ThreadContext thread)
                => new ThreadAction(thread, ActionKind.Create, null);

            public static ThreadAction Terminate(ThreadContext thread)
                => new ThreadAction(thread, ActionKind.Terminate, null);

            public static ThreadAction Suspend(ThreadContext thread, TimeSpan? timeout)
                => new ThreadAction(thread, ActionKind.Suspend, timeout);

            public static ThreadAction Resume(ThreadContext thread)
                => new ThreadAction(thread, ActionKind.Resume, null);

            private ThreadAction(ThreadContext thread, ActionKind kind, TimeSpan? timeout)
            {
                Thread = thread;
                Kind = kind;
                Timeout = timeout;
            }
        }

        internal class SystemVariableLookup
        {
            private readonly VirtualMachine _vm;
            private readonly GlobalVarLookupTable _nameLookup;

            public readonly int PresentPreprocess;
            public readonly int PresentText;
            public readonly int PresentProcess;

            public SystemVariableLookup(VirtualMachine vm)
            {
                _vm = vm;
                _nameLookup = vm._globalVarLookup;
                PresentPreprocess = Lookup("SYSTEM_present_preprocess");
                PresentText = Lookup("SYSTEM_present_text");
                PresentProcess = Lookup("SYSTEM_present_process");
            }

            private int Lookup(string name)
            {
                _nameLookup.TryLookupSystemVariable(name, out int index);
                return index;
            }

            public ref ConstantValue CurrentSubroutineName => ref Var(PresentProcess);
            public ref ConstantValue CurrentBoxName => ref Var(PresentPreprocess);
            public ref ConstantValue CurrentTextName => ref Var(PresentText);

            private ref ConstantValue Var(int index) => ref _vm.GetGlobalVar(index);
        }

        private readonly NsxModuleLocator _moduleLocator;
        private readonly BuiltInFunctions _builtInFuncImpl;
        private readonly Dictionary<string, NsxModule> _loadedModules;
        private readonly BuiltInFunctionDispatcher _builtInCallDispatcher;
        private readonly List<ThreadContext> _threads;
        private readonly Dictionary<string, ThreadContext> _threadMap;
        private readonly Queue<ThreadAction> _pendingThreadActions;
        private readonly Stopwatch _timer;
        private readonly ConstantValue[] _globals;
        private readonly GlobalVarLookupTable _globalVarLookup;
        private readonly SystemVariableLookup _systemVariables;

        public ThreadContext? MainThread { get; internal set; }
        public ThreadContext? CurrentThread { get; internal set; }

        public VirtualMachine(
            NsxModuleLocator moduleLocator,
            Stream globalVarLookupTableStream,
            BuiltInFunctions builtInFunctionsImpl)
        {
            _loadedModules = new Dictionary<string, NsxModule>(16);
            _moduleLocator = moduleLocator;
            _builtInFuncImpl = builtInFunctionsImpl;
            _builtInCallDispatcher = new BuiltInFunctionDispatcher(builtInFunctionsImpl);
            _threads = new List<ThreadContext>();
            _threadMap = new Dictionary<string, ThreadContext>();
            _pendingThreadActions = new Queue<ThreadAction>();
            _timer = Stopwatch.StartNew();
            _globals = new ConstantValue[5000];
            _globalVarLookup = GlobalVarLookupTable.Load(globalVarLookupTableStream);
            builtInFunctionsImpl._vm = this;
            _systemVariables = new SystemVariableLookup(this);
        }

        public IReadOnlyList<ThreadContext> Threads => _threads;

        public ThreadContext CreateThread(string name, string symbol, bool start = false)
            => CreateThread(name, CurrentThread!.CurrentFrame.Module.Name, symbol, start);

        public void ActivateDialogueBlock(in DialogueBlockToken blockToken)
        {
            var frame = new CallFrame(
                blockToken.Module, (ushort)blockToken.SubroutineIndex, 0, 0);
            frame.ProgramCounter = blockToken.Offset;

            CurrentThread!.CallFrameStack.Push(frame);
        }

        public ThreadContext CreateThread(string name, string moduleName, string symbol, bool start = true)
        {
            NsxModule module = GetModule(moduleName);
            ushort subIndex = (ushort)module.LookupSubroutineIndex(symbol);
            var frame = new CallFrame(module, subIndex, 0, 0);
            var thread = new ThreadContext(name, ref frame);
            _pendingThreadActions.Enqueue(ThreadAction.Create(thread));
            if (MainThread == null)
            {
                MainThread = thread;
            }

            if (!start)
            {
                CommitSuspendThread(thread, null);
            }

            return thread;
        }

        public NsxModule GetModule(string name)
        {
            if (!_loadedModules.TryGetValue(name, out NsxModule? module))
            {
                Stream stream = _moduleLocator.OpenModule(name);
                module = NsxModule.LoadModule(stream, name);
                _loadedModules.Add(name, module);
            }

            return module;
        }

        public void SuspendThread(ThreadContext thread, TimeSpan? timeout = null)
        {
            _pendingThreadActions.Enqueue(ThreadAction.Suspend(thread, timeout));
        }

        public void ResumeThread(ThreadContext thread)
        {
            _pendingThreadActions.Enqueue(ThreadAction.Resume(thread));
        }

        public void TerminateThread(ThreadContext thread)
        {
            _pendingThreadActions.Enqueue(ThreadAction.Terminate(thread));
        }

        public void SuspendMainThread()
        {
            Debug.Assert(MainThread != null);
            SuspendThread(MainThread);
        }

        public void ResumeMainThread()
        {
            Debug.Assert(MainThread != null);
            ResumeThread(MainThread);
        }

        public bool RefreshThreadState()
        {
            int nbResumed = 0;
            long? time = null;
            foreach (ThreadContext thread in _threads)
            {
                if (thread.SuspensionTime != null && thread.SleepTimeout != null)
                {
                    if (time == null)
                    {
                        time = _timer.ElapsedTicks;
                    }

                    long delta = time.Value - thread.SuspensionTime.Value;
                    if (delta >= thread.SleepTimeout)
                    {
                        CommitResumeThread(thread);
                        nbResumed++;
                    }
                }
            }
            return nbResumed > 0;
        }

        public bool TryGetThread(string name, [NotNullWhen(true)] out ThreadContext? thread)
        {
            return _threadMap.TryGetValue(name, out thread);
        }

        public bool Run(CancellationToken cancellationToken)
        {
            bool result = false;
            while (_threads.Count > 0 || _pendingThreadActions.Count > 0)
            {
                ProcessPendingThreadActions();
                int nbActive = 0;
                foreach (ThreadContext thread in _threads)
                {
                    if (thread.IsActive)
                    {
                        CurrentThread = thread;
                        nbActive++;
                        result = true;
                        Tick(thread);
                        if (thread.DoneExecuting)
                        {
                            TerminateThread(thread);
                            nbActive--;
                        }
                    }
                }

                if (nbActive == 0)
                {
                    return result;
                }
            }

            return result;
        }

        public bool ProcessPendingThreadActions()
        {
            bool result = false;
            while (_pendingThreadActions.TryDequeue(out ThreadAction action))
            {
                ThreadContext thread = action.Thread;
                switch (action.Kind)
                {
                    case ThreadAction.ActionKind.Create:
                        if (_threadMap.TryAdd(thread.Name, thread))
                        {
                            _threads.Add(thread);
                            result = true;
                        }
                        break;
                    case ThreadAction.ActionKind.Terminate:
                        CommitTerminateThread(thread);
                        break;
                    case ThreadAction.ActionKind.Suspend:
                        CommitSuspendThread(thread, action.Timeout);
                        break;
                    case ThreadAction.ActionKind.Resume:
                        CommitResumeThread(thread);
                        result = true;
                        break;
                }
            }

            return result;
        }

        private void CommitSuspendThread(ThreadContext thread, TimeSpan? timeout)
        {
            thread.SuspensionTime = TicksFromTimeSpan(_timer.Elapsed);
            if (timeout != null)
            {
                thread.SleepTimeout = TicksFromTimeSpan(timeout.Value);
            }
        }

        private void CommitResumeThread(ThreadContext thread)
        {
            thread.SleepTimeout = null;
            thread.SuspensionTime = null;
        }

        private void CommitTerminateThread(ThreadContext thread)
        {
            _threads.Remove(thread);
            _threadMap.Remove(thread.Name);
        }

        private static long TicksFromTimeSpan(TimeSpan timespan)
            => (long)(timespan.TotalSeconds * Stopwatch.Frequency);

        private ref ConstantValue GetGlobalVar(int index)
        {
            ref ConstantValue val = ref _globals[index];
            if (val.Type == BuiltInType.Uninitialized)
            {
                val = ConstantValue.Integer(0);
            }

            return ref val;
        }

        private void Tick(ThreadContext thread)
        {
            if (thread.CallFrameStack.Count == 0)
            {
                return;
            }

            ref CallFrame frame = ref thread.CurrentFrame;
            NsxModule thisModule = frame.Module;
            Subroutine subroutine = thisModule.GetSubroutine(frame.SubroutineIndex);
            var program = new BytecodeStream(subroutine.Code);
            program.Position = frame.ProgramCounter;
            ref ValueStack<ConstantValue> stack = ref thread.EvalStack;
            while (true)
            {
                Opcode opcode = program.NextOpcode();
                ushort varToken = 0;
                ConstantValue? imm = opcode switch
                {
                    Opcode.LoadImm => readConst(ref program, thisModule),
                    Opcode.LoadImm0 => ConstantValue.Integer(0),
                    Opcode.LoadImm1 => ConstantValue.Integer(1),
                    Opcode.LoadImmTrue => ConstantValue.True,
                    Opcode.LoadImmFalse => ConstantValue.False,
                    Opcode.LoadImmNull => ConstantValue.Null,
                    Opcode.LoadImmEmptyStr => ConstantValue.EmptyString,
                    Opcode.LoadVar => GetGlobalVar((varToken = program.DecodeToken())),
                    Opcode.LoadArg0 => stack[frame.ArgStart + 0],
                    Opcode.LoadArg1 => stack[frame.ArgStart + 1],
                    Opcode.LoadArg2 => stack[frame.ArgStart + 2],
                    Opcode.LoadArg3 => stack[frame.ArgStart + 3],
                    Opcode.LoadArg => stack[frame.ArgStart + program.ReadByte()],
                    _ => (ConstantValue?)null
                };

                if (imm.HasValue)
                {
                    if (varToken != 0 && varToken == _systemVariables.PresentProcess)
                    {
                        string subName = thisModule.GetSubroutineName(frame.SubroutineIndex);
                        imm = _globals[varToken] = ConstantValue.String(subName);
                    }

                    ConstantValue value = imm.Value;
                    stack.Push(ref value);
                    continue;
                }

                switch (opcode)
                {
                    case Opcode.StoreVar:
                        int index = program.DecodeToken();
                        GetGlobalVar(index) = stack.Pop();
                        break;
                    case Opcode.StoreArg0:
                        stack[frame.ArgStart + 0] = stack.Pop();
                        break;
                    case Opcode.StoreArg1:
                        stack[frame.ArgStart + 1] = stack.Pop();
                        break;
                    case Opcode.StoreArg2:
                        stack[frame.ArgStart + 2] = stack.Pop();
                        break;
                    case Opcode.StoreArg3:
                        stack[frame.ArgStart + 3] = stack.Pop();
                        break;
                    case Opcode.StoreArg:
                        stack[frame.ArgStart + program.ReadByte()] = stack.Pop();
                        break;

                    case Opcode.Binary:
                        var opKind = (BinaryOperatorKind)program.ReadByte();
                        ConstantValue op2 = stack.Pop();
                        ConstantValue op1 = stack.Pop();
                        stack.Push(BinOp(op1, opKind, op2));
                        break;
                    case Opcode.Equal:
                        op2 = stack.Pop();
                        op1 = stack.Pop();
                        stack.Push(op1 == op2);
                        break;
                    case Opcode.NotEqual:
                        op2 = stack.Pop();
                        op1 = stack.Pop();
                        stack.Push(op1 != op2);
                        break;

#pragma warning disable IDE0059
                    case Opcode.Neg:
                        ref ConstantValue val = ref stack.Peek();
                        val = val.Type switch
                        {
                            BuiltInType.Integer => ConstantValue.Integer(-val.AsInteger()!.Value),
                            BuiltInType.Float => ConstantValue.Float(-val.AsFloat()!.Value),
                            _ => ThrowHelper.Unreachable<ConstantValue>()
                        };
                        break;
                    case Opcode.Inc:
                        val = ref stack.Peek();
                        Debug.Assert(val.Type == BuiltInType.Integer); // TODO: runtime error
                        val = ConstantValue.Integer(val.AsInteger()!.Value + 1);
                        break;
                    case Opcode.Dec:
                        val = ref stack.Peek();
                        Debug.Assert(val.Type == BuiltInType.Integer); // TODO: runtime error
                        val = ConstantValue.Integer(val.AsInteger()!.Value - 1);
                        break;
                    case Opcode.Delta:
                        val = ref stack.Peek();
                        Debug.Assert(val.Type == BuiltInType.Integer);
                        val = ConstantValue.Delta(val.AsInteger()!.Value);
                        break;
                    case Opcode.Invert:
                        val = ref stack.Peek();
                        Debug.Assert(val.AsBool() != null);
                        val = ConstantValue.Boolean(!val.AsBool()!.Value);
                        break;
#pragma warning restore IDE0059

                    case Opcode.Call:
                        ushort subroutineToken = program.DecodeToken();
                        ushort argCount = program.ReadByte();
                        ushort argStart = (ushort)(stack.Count - argCount);
                        frame.ProgramCounter = program.Position;
                        var newFrame = new CallFrame(frame.Module, subroutineToken, argStart, argCount);
                        thread.CallFrameStack.Push(newFrame);
                        if (CurrentThread == MainThread)
                        {
                            string name = thisModule.GetSubroutineRuntimeInformation(subroutineToken).SubroutineName;
                            for (int i = 0; i < thread.CallFrameStack.Count; i++)
                            {
                                Console.Write(" ");
                            }
                            Console.WriteLine("near: " + name);
                        }
                        return;
                    case Opcode.CallFar:
                        ushort importTableIndex = program.DecodeToken();
                        subroutineToken = program.DecodeToken();
                        argCount = program.ReadByte();
                        argStart = (ushort)(stack.Count - argCount);
                        string externalModuleName = thisModule.Imports[importTableIndex];
                        NsxModule externalModule = GetModule(externalModuleName);
                        newFrame = new CallFrame(externalModule, subroutineToken, argStart, argCount);
                        thread.CallFrameStack.Push(newFrame);
                        frame.ProgramCounter = program.Position;
                        if (CurrentThread == MainThread)
                        {
                            string name = externalModule.GetSubroutineRuntimeInformation(subroutineToken).SubroutineName;
                            for (int i = 0; i < thread.CallFrameStack.Count; i++)
                            {
                                Console.Write(" ");
                            }
                            Console.WriteLine("far: " + name);
                        }
                        return;
                    case Opcode.Jump:
                        int @base = program.Position - 1;
                        int offset = program.DecodeOffset();
                        program.Position = @base + offset;
                        break;
                    case Opcode.JumpIfTrue:
                        @base = program.Position - 1;
                        ConstantValue condition = stack.Pop();
                        offset = program.DecodeOffset();
                        Debug.Assert(condition.Type == BuiltInType.Boolean);
                        if (condition.AsBool()!.Value)
                        {
                            program.Position = @base + offset;
                        }
                        break;
                    case Opcode.JumpIfFalse:
                        @base = program.Position - 1;
                        condition = stack.Pop();
                        offset = program.DecodeOffset();
                        if (!condition.AsBool()!.Value)
                        {
                            program.Position = @base + offset;
                        }
                        break;
                    case Opcode.Return:
                        if (thread.CallFrameStack.Count == 0) { return; }
                        thread.CallFrameStack.Pop();
                        return;
                    case Opcode.Dispatch:
                        var func = (BuiltInFunction)program.ReadByte();
                        argCount = program.ReadByte();
                        ReadOnlySpan<ConstantValue> args = stack.AsSpan(stack.Count - argCount, argCount);
                        switch (func)
                        {
                            default:
                                if (CurrentThread == MainThread)
                                {
                                    Console.Write($"Built-in: {func.ToString()}(");
                                    foreach (ref readonly ConstantValue cv in args)
                                    {
                                        Console.Write(cv.ConvertToString() + ", ");
                                    }
                                    Console.Write(")\r\n");
                                }
                                ConstantValue? result = _builtInCallDispatcher.Dispatch(func, args);
                                stack.Pop(argCount);
                                if (result != null)
                                {
                                    stack.Push(result.Value);
                                }
                                break;

                            case BuiltInFunction.log:
                                ConstantValue arg = stack.Pop();
                                Console.WriteLine($"[VM]: {arg.ConvertToString()}");
                                break;
                            case BuiltInFunction.assert:
                                arg = stack.Pop();
                                if (arg.AsBool()!.Value == false)
                                {
                                    string subrName = thisModule.GetSubroutineRuntimeInformation(
                                        frame.SubroutineIndex).SubroutineName;
                                    Console.WriteLine($"{subrName} + {program.Position - 1}: assertion failed.");
                                }
                                break;
                            case BuiltInFunction.asserteq:
                                ConstantValue expected = stack.Pop();
                                ConstantValue actual = stack.Pop();
                                if (expected != actual)
                                {
                                    string subrName = thisModule.GetSubroutineRuntimeInformation(
                                        frame.SubroutineIndex).SubroutineName;
                                    Console.WriteLine($"{subrName} + {program.Position - 1}: assertion failed.");
                                }
                                break;
                            case BuiltInFunction.fail:
                                string subName = thisModule.GetSubroutineRuntimeInformation(
                                    frame.SubroutineIndex).SubroutineName;
                                Console.WriteLine($"{subName} + {program.Position - 1}: test failed.");
                                break;
                            case BuiltInFunction.fail_msg:
                                ConstantValue message = stack.Pop();
                                subName = thisModule.GetSubroutineRuntimeInformation(
                                        frame.SubroutineIndex).SubroutineName;
                                Console.WriteLine($"{subName} + {program.Position - 1}: {message.ToString()}.");
                                break;
                        }
                        frame.ProgramCounter = program.Position;
                        return;

                    case Opcode.ActivateText:
                        ushort textId = program.DecodeToken();
                        ref readonly var srti = ref thisModule.GetSubroutineRuntimeInformation(
                            frame.SubroutineIndex);
                        (string box, string textName) = srti.DialogueBlockInfos[textId];
                        _systemVariables.CurrentBoxName = ConstantValue.String(box);
                        _systemVariables.CurrentTextName = ConstantValue.String(textName);
                        break;

                    case Opcode.Select:
                        _builtInFuncImpl.Select();
                        frame.ProgramCounter = program.Position;
                        return;
                    case Opcode.GetSelChoice:
                        string choice = _builtInFuncImpl.GetSelectedChoice() ?? string.Empty;
                        stack.Push(ConstantValue.String(choice));
                        break;
                    case Opcode.PresentText:
                        string text = thisModule.GetString(program.DecodeToken());
                        _builtInFuncImpl.BeginDialogueLine(text);
                        break;
                    case Opcode.AwaitInput:
                        _builtInFuncImpl.WaitForInput();
                        frame.ProgramCounter = program.Position;
                        return;
                }
            }

            static ConstantValue readConst(ref BytecodeStream stream, NsxModule module)
            {
                Immediate imm = stream.DecodeImmediateValue();
                return imm.Type switch
                {
                    BuiltInType.Integer => ConstantValue.Integer(imm.IntegerValue),
                    BuiltInType.DeltaInteger => ConstantValue.Delta(imm.IntegerValue),
                    BuiltInType.BuiltInConstant => ConstantValue.BuiltInConstant(imm.Constant),
                    BuiltInType.String => ConstantValue.String(module.GetString(imm.StringToken)),
                    _ => ThrowHelper.Unreachable<ConstantValue>()
                };
            }
        }

        private static ConstantValue BinOp(
            in ConstantValue left,
            BinaryOperatorKind opKind,
            in ConstantValue right)
        {
            return opKind switch
            {
                BinaryOperatorKind.Add => left + right,
                BinaryOperatorKind.Subtract => left - right,
                BinaryOperatorKind.Multiply => left * right,
                BinaryOperatorKind.Divide => left / right,
                BinaryOperatorKind.LessThan => left < right,
                BinaryOperatorKind.LessThanOrEqual => left <= right,
                BinaryOperatorKind.GreaterThan => left > right,
                BinaryOperatorKind.GreaterThanOrEqual => left >= right,
                BinaryOperatorKind.And => left && right,
                BinaryOperatorKind.Or => left || right,
                BinaryOperatorKind.Remainder => left & right,
                _ => throw new NotImplementedException()
            };
        }
    }
}
