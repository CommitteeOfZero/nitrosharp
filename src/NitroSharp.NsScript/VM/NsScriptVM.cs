using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM
{
    public readonly ref struct RunResult
    {
        public readonly ReadOnlySpan<uint> NewThreads;
        public readonly ReadOnlySpan<uint> TerminatedThreads;

        public RunResult(
            ReadOnlySpan<uint> newThreads,
            ReadOnlySpan<uint> terminatedThreads)
        {
            NewThreads = newThreads;
            TerminatedThreads = terminatedThreads;
        }
    }

    [Persistable]
    public readonly partial struct GlobalsDump
    {
        internal (string, ConstantValue)[] Globals { get; init; }
    }

    public sealed class NsScriptVM
    {
        private readonly NsxModuleLocator _moduleLocator;
        private readonly Dictionary<string, NsxModule> _loadedModules;
        private readonly BuiltInFunctionDispatcher _builtInCallDispatcher;
        private readonly ConstantValue[] _variables;
        private readonly ConstantValue[] _flags;
        private readonly Stack<CubicBezierSegment> _bezierSegmentStack;

        private uint _lastProcessId;
        private uint _lastThreadId;

        public NsScriptVM(
            NsxModuleLocator moduleLocator,
            Stream globalsLookupTableStream)
        {
            _loadedModules = new Dictionary<string, NsxModule>(16);
            _moduleLocator = moduleLocator;
            _variables = new ConstantValue[5000];
            _flags = new ConstantValue[2000];
            _builtInCallDispatcher = new BuiltInFunctionDispatcher(_variables);
            GlobalsLookup = GlobalsLookupTable.Load(globalsLookupTableStream);
            SystemVariables = new SystemVariableLookup(this);
            _bezierSegmentStack = new Stack<CubicBezierSegment>();
        }

        internal GlobalsLookupTable GlobalsLookup { get; }

        public SystemVariableLookup SystemVariables { get; }
        public NsScriptProcess? CurrentProcess { get; private set; }

        public NsScriptProcess RestoreProcess(in NsScriptProcessDump dump)
        {
            NsScriptProcess process = new(this, dump);
            _lastProcessId = Math.Max(_lastProcessId, process.Id);
            _lastThreadId = Math.Max(_lastThreadId, dump.Threads.Max(x => x.Id));
            return process;
        }

        public GlobalsDump DumpVariables() => DumpGlobals(_variables, GlobalsLookup.Variables);
        public GlobalsDump DumpFlags() => DumpGlobals(_flags, GlobalsLookup.Flags);

        public void RestoreVariables(GlobalsDump dump)
            => RestoreGlobals(_variables, GlobalsLookup.Variables, dump);

        public void RestoreFlags(GlobalsDump dump)
            => RestoreGlobals(_flags, GlobalsLookup.Flags, dump);

        private static GlobalsDump DumpGlobals(
            ConstantValue[] table,
            ImmutableDictionary<string, int> lookup)
        {
            var globals = new (string, ConstantValue)[lookup.Count];
            foreach ((string name, int i) in lookup)
            {
                globals[i] = (name, table[i]);
            }
            return new GlobalsDump { Globals = globals };
        }

        private static void RestoreGlobals(
            ConstantValue[] table,
            ImmutableDictionary<string, int> lookup,
            in GlobalsDump dump)
        {
            foreach ((string name, ConstantValue val) in dump.Globals)
            {
                if (lookup.TryGetValue(name, out int index))
                {
                    table[index] = val;
                }
            }
        }

        internal NsxModule GetModule(string name)
        {
            if (!_loadedModules.TryGetValue(name, out NsxModule? module))
            {
                Stream stream = _moduleLocator.OpenModule(name);
                module = NsxModule.LoadModule(stream, name);
                _loadedModules.Add(name, module);
            }

            return module;
        }

        public NsScriptProcess CreateProcess(string moduleName, string symbol)
        {
            uint pid = ++_lastProcessId;
            NsScriptThread mainThread = CreateThread(moduleName, symbol);
            return new NsScriptProcess(this, pid, mainThread);
        }

        public NsScriptThread CreateThread(NsScriptProcess process, string symbol, bool start = false)
            => CreateThread(process, process.CurrentThread!.CurrentFrame.Module.Name, symbol, start);

        public NsScriptThread CreateThread(
            NsScriptProcess process,
            string moduleName,
            string symbol,
            bool start)
        {
            NsScriptThread thread = CreateThread(moduleName, symbol);
            process.AttachThread(thread);
            if (!start)
            {
                process.CommitSuspendThread(thread, null);
            }
            return thread;
        }

        private NsScriptThread CreateThread(
            NsScriptProcess process,
            ref CallFrame callFrame,
            uint? declaredId = null)
        {
            var thread = new NsScriptThread(++_lastThreadId, ref callFrame, declaredId);
            process.AttachThread(thread);
            return thread;
        }

        private NsScriptThread CreateThread(string moduleName, string symbol)
        {
            NsxModule module = GetModule(moduleName);
            ushort subIndex = (ushort)module.LookupSubroutineIndex(symbol);
            var frame = new CallFrame(module, subIndex, 0);
            return new NsScriptThread(++_lastThreadId, ref frame);
        }

        public void SuspendThread(NsScriptThread thread, TimeSpan? timeout = null)
        {
            thread.Process.PendingThreadActions
                .Enqueue(ThreadAction.Suspend(thread, timeout));
        }

        public void Join(NsScriptThread callingThread, NsScriptThread targetThread)
        {
            callingThread.Process.PendingThreadActions
                .Enqueue(ThreadAction.Join(callingThread, targetThread));
        }

        public void ResumeThread(NsScriptThread thread)
        {
            thread.Process.PendingThreadActions
                .Enqueue(ThreadAction.Resume(thread));
        }

        public void TerminateThread(NsScriptThread thread)
        {
            thread.Process.PendingThreadActions
                .Enqueue(ThreadAction.Terminate(thread));
        }

        public NsScriptThread ActivateDialogueBlock(in DialogueBlockToken blockToken)
            => ActivateDialogueBlock(CurrentProcess!, blockToken);

        public NsScriptThread ActivateDialogueBlock(
            NsScriptProcess process,
            in DialogueBlockToken blockToken)
        {
            var frame = new CallFrame(
                blockToken.Module,
                (ushort)blockToken.SubroutineIndex,
                pc: blockToken.Offset
            );
            NsScriptThread thread = CreateThread(process, ref frame, declaredId: process.CurrentThread!.Id);
            thread.DialoguePage = new EntityPath("@" + blockToken.BlockName);
            return thread;
        }

        public RunResult Run(
            NsScriptProcess process,
            BuiltInFunctions builtins,
            CancellationToken cancellationToken)
        {
            builtins._vm = this;
            CurrentProcess = process;
            process.Tick();

            while (process.IsRunning
                && (!process.Threads.IsEmpty || process.PendingThreadActions.Count > 0))
            {
                process.ProcessPendingThreadActions();
                uint nbActive = 0;
                foreach (NsScriptThread thread in process.Threads)
                {
                    if (!process.IsRunning) { break; }
                    if (thread.IsActive && !thread.Yielded)
                    {
                        process.CurrentThread = thread;
                        nbActive++;
                        TickResult tickResult = Tick(process, thread, builtins);
                        if (!process.IsRunning) { break; }
                        if (tickResult == TickResult.Yield)
                        {
                            thread.Yielded = true;
                            nbActive--;
                        }
                        else if (thread.DoneExecuting)
                        {
                            if (thread.WaitingThread is
                                NsScriptThread { DoneExecuting: false } waitingThread)
                            {
                                ResumeThread(waitingThread);
                                nbActive++;
                            }
                            TerminateThread(thread);
                            nbActive--;
                        }
                    }
                }

                if (nbActive == 0)
                {
                    foreach (NsScriptThread thread in process.Threads)
                    {
                        thread.Yielded = false;
                    }
                    break;
                }
            }

            return new RunResult(process.NewThreads, process.TerminatedThreads);
        }

        internal ref ConstantValue GetVariable(int index)
        {
            ref ConstantValue val = ref _variables[index];
            if (val.Type == BuiltInType.Uninitialized)
            {
                val = ConstantValue.Number(0);
            }

            return ref val;
        }

        internal ref ConstantValue GetFlag(int index)
        {
            ref ConstantValue val = ref _flags[index];
            if (val.Type == BuiltInType.Uninitialized)
            {
                val = ConstantValue.Number(0);
            }

            return ref val;
        }

        private enum TickResult
        {
            Ok,
            Yield
        }

        private TickResult Tick(NsScriptProcess process, NsScriptThread thread, BuiltInFunctions builtins)
        {
            if (thread.CallFrameStack.Count == 0)
            {
                return TickResult.Ok;
            }

            ref CallFrame frame = ref thread.CurrentFrame;
            NsxModule thisModule = frame.Module;
            Subroutine subroutine = thisModule.GetSubroutine(frame.SubroutineIndex);
            var program = new BytecodeStream(subroutine.Code, frame.ProgramCounter);
            ref ValueStack<ConstantValue> stack = ref thread.EvalStack;
            while (true)
            {
                Opcode opcode = program.NextOpcode();
                ushort varToken = ushort.MaxValue;
                ushort flagToken;
                ConstantValue? imm = opcode switch
                {
                    Opcode.LoadImm => readConst(ref program, thisModule),
                    Opcode.LoadImm0 => ConstantValue.Number(0),
                    Opcode.LoadImm1 => ConstantValue.Number(1),
                    Opcode.LoadImmTrue => ConstantValue.True,
                    Opcode.LoadImmFalse => ConstantValue.False,
                    Opcode.LoadImmNull => ConstantValue.Null,
                    Opcode.LoadImmEmptyStr => ConstantValue.EmptyString,
                    Opcode.LoadVar => GetVariable(varToken = program.DecodeToken())
                        .WithSlot((short)varToken),
                    Opcode.LoadFlag => GetFlag(flagToken = program.DecodeToken())
                        .WithSlot((short)flagToken),
                    _ => null
                };

                if (imm.HasValue)
                {
                    if (varToken != ushort.MaxValue && varToken == SystemVariables.PresentProcess)
                    {
                        string subName = thisModule.GetSubroutineName(frame.SubroutineIndex);
                        imm = _variables[varToken] = ConstantValue.String(subName);
                    }

                    ConstantValue value = imm.Value;
                    stack.Push(ref value);
                    continue;
                }

                switch (opcode)
                {
                    case Opcode.StoreVar:
                        int index = program.DecodeToken();
                        GetVariable(index) = stack.Pop();
                        break;
                    case Opcode.StoreFlag:
                        index = program.DecodeToken();
                        GetFlag(index) = stack.Pop();
                        break;
                    case Opcode.Binary:
                        var opKind = (BinaryOperatorKind)program.ReadByte();
                        ConstantValue op1 = stack.Pop();
                        ConstantValue op2 = stack.Pop();
                        stack.Push(BinOp(op1, opKind, op2));
                        break;
                    case Opcode.Equal:
                        op1 = stack.Pop();
                        op2 = stack.Pop();
                        stack.Push(op1 == op2);
                        break;
                    case Opcode.NotEqual:
                        op1 = stack.Pop();
                        op2 = stack.Pop();
                        stack.Push(op1 != op2);
                        break;
                    case Opcode.Neg:
                        ref ConstantValue val = ref stack.Peek();
                        val = val.Type switch
                        {
                            BuiltInType.Numeric => ConstantValue.Number(-val.AsNumber()!.Value),
                            _ => ThrowHelper.Unreachable<ConstantValue>()
                        };
                        break;
                    case Opcode.Inc:
                        val = ref stack.Peek();
                        Debug.Assert(val.Type == BuiltInType.Numeric); // TODO: runtime error
                        val = ConstantValue.Number(val.AsNumber()!.Value + 1);
                        break;
                    case Opcode.Dec:
                        val = ref stack.Peek();
                        Debug.Assert(val.Type == BuiltInType.Numeric); // TODO: runtime error
                        val = ConstantValue.Number(val.AsNumber()!.Value - 1);
                        break;
                    case Opcode.Delta:
                        val = ref stack.Peek();
                        if (val.AsNumber() is float num)
                        {
                            val = ConstantValue.Delta(num);
                        }
                        else
                        {
                            val = ConstantValue.String("@" + val.AsString()!);
                        }
                        break;
                    case Opcode.Invert:
                        val = ref stack.Peek();
                        Debug.Assert(val.AsBool() != null);
                        val = ConstantValue.Boolean(!val.AsBool()!.Value);
                        break;
                    case Opcode.Call:
                        ushort subroutineToken = program.DecodeToken();
                        frame.ProgramCounter = program.Position;
                        var newFrame = new CallFrame(frame.Module, subroutineToken, 0);
                        thread.CallFrameStack.Push(newFrame);
                        if (process.CurrentThread == process.MainThread)
                        {
                            //string name = thisModule.GetSubroutineRuntimeInfo(subroutineToken)
                            //    .SubroutineName;
                            //for (int i = 0; i < thread.CallFrameStack.Count; i++)
                            //{
                            //    Console.Write(" ");
                            //}
                            //Console.WriteLine("near: " + name);
                        }
                        return TickResult.Ok;
                    case Opcode.CallFar:
                        newFrame = externalCall(ref program);
                        thread.CallFrameStack.Push(newFrame);
                        frame.ProgramCounter = program.Position;
                        if (process.CurrentThread == process.MainThread)
                        {
                            string name = newFrame.Module
                                .GetSubroutineRuntimeInfo(newFrame.SubroutineIndex)
                                .SubroutineName;
                            for (int i = 0; i < thread.CallFrameStack.Count; i++)
                            {
                                Console.Write(" ");
                            }
                            Console.WriteLine("far: " + name);
                        }
                        return TickResult.Ok;
                    case Opcode.CallScene:
                        newFrame = externalCall(ref program);
                        NsScriptThread newThread = CreateThread(process, ref newFrame);
                        Join(thread, newThread);
                        ResumeThread(newThread);
                        frame.ProgramCounter = program.Position;
                        return TickResult.Ok;
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
                        if (thread.CallFrameStack.Count > 0)
                        {
                            thread.CallFrameStack.Pop();
                        }
                        return TickResult.Ok;
                    case Opcode.BezierStart:
                        _bezierSegmentStack.Clear();
                        break;
                    case Opcode.BezierEndSeg:
                        static BezierControlPoint popPoint(ref ValueStack<ConstantValue> stack)
                        {
                            var x = NsCoordinate.FromValue(stack.Pop());
                            var y = NsCoordinate.FromValue(stack.Pop());
                            return new BezierControlPoint(x, y);
                        }

                        var seg = new CubicBezierSegment(
                            popPoint(ref stack),
                            popPoint(ref stack),
                            popPoint(ref stack),
                            popPoint(ref stack)
                        );
                        _bezierSegmentStack.Push(seg);
                        break;
                    case Opcode.BezierEnd:
                        var curve = new CompositeBezier(_bezierSegmentStack.ToImmutableArray());
                        stack.Push(ConstantValue.BezierCurve(curve));
                        break;
                    case Opcode.Dispatch:
                        var func = (BuiltInFunction)program.ReadByte();
                        int argCount = program.ReadByte();
                        ReadOnlySpan<ConstantValue> args = stack.AsSpan(stack.Count - argCount, argCount);
                        switch (func)
                        {
                            default:
                                if (process.CurrentThread == process.MainThread)
                                {
                                    //Console.Write($"Built-in: {func.ToString()}(");
                                    //foreach (ref readonly ConstantValue cv in args)
                                    //{
                                    //    Console.Write(cv.ConvertToString() + ", ");
                                    //}
                                    //Console.Write(")\r\n");
                                }
                                ConstantValue? result = _builtInCallDispatcher.Dispatch(builtins, func, args);
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
                            case BuiltInFunction.fail:
                                string subName = thisModule.GetSubroutineRuntimeInfo(
                                    frame.SubroutineIndex).SubroutineName;
                                Console.WriteLine($"{subName} + {program.Position - 1}: test failed.");
                                break;
                            case BuiltInFunction.fail_msg:
                                ConstantValue message = stack.Pop();
                                subName = thisModule.GetSubroutineRuntimeInfo(frame.SubroutineIndex)
                                    .SubroutineName;
                                Console.WriteLine($"{subName} + {program.Position - 1}: {message.ToString()}.");
                                break;
                        }
                        frame.ProgramCounter = program.Position;
                        return TickResult.Ok;

                    case Opcode.ActivateBlock:
                        ushort blockId = program.DecodeToken();
                        ref readonly var srti = ref thisModule.GetSubroutineRuntimeInfo(
                            frame.SubroutineIndex
                        );
                        (string box, string textName) = srti.DialogueBlockInfos[blockId];
                        SystemVariables.CurrentDialogueBox = ConstantValue.String(box);
                        SystemVariables.CurrentDialogueBlock = ConstantValue.String("@" + textName);
                        break;
                    case Opcode.ClearPage:
                        Debug.Assert(thread.DialoguePage.HasValue);
                        builtins.ClearDialoguePage(thread.DialoguePage.Value);
                        break;
                    case Opcode.AppendDialogue:
                        Debug.Assert(thread.DialoguePage.HasValue);
                        string text = thisModule.GetString(program.DecodeToken());
                        builtins.AppendDialogue(thread.DialoguePage.Value, text);
                        break;
                    case Opcode.LineEnd:
                        Debug.Assert(thread.DialoguePage.HasValue);
                        builtins.LineEnd(thread.DialoguePage.Value);
                        frame.ProgramCounter = program.Position;
                        return TickResult.Ok;

                    case Opcode.SelectLoopStart:
                        thread.SelectResult = false;
                        break;
                    case Opcode.IsPressed:
                        string choice = thisModule.GetString(program.DecodeToken());
                        bool pressed = builtins.HandleInputEvents(new EntityPath(choice));
                        stack.Push(ConstantValue.Boolean(pressed));
                        thread.SelectResult |= pressed;
                        break;
                    case Opcode.SelectLoopEnd:
                        stack.Push(ConstantValue.Boolean(thread.SelectResult));
                        frame.ProgramCounter = program.Position;
                        return TickResult.Yield;
                    case Opcode.SelectEnd:
                        builtins.SelectEnd();
                        break;
                }
            }

            CallFrame externalCall(ref BytecodeStream program)
            {
                ushort importTableIndex = program.DecodeToken();
                ushort subroutineToken = program.DecodeToken();
                string externalModuleName = thisModule.Imports[importTableIndex];
                NsxModule externalModule = GetModule(externalModuleName);
                return new CallFrame(externalModule, subroutineToken, 0);
            }

            static ConstantValue readConst(ref BytecodeStream stream, NsxModule module)
            {
                Immediate imm = stream.DecodeImmediateValue();
                return imm.Type switch
                {
                    BuiltInType.Numeric => ConstantValue.Number(imm.Numeric),
                    BuiltInType.DeltaNumeric => ConstantValue.Delta(imm.Numeric),
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
            if (right is { Type: BuiltInType.Numeric } && right.AsNumber() is 300 && left.Type is not BuiltInType.Numeric)
            {
                Debugger.Break();
            }

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
                BinaryOperatorKind.Remainder => left % right,
                _ => throw new NotImplementedException()
            };
        }
    }

    public sealed class SystemVariableLookup
    {
        private readonly NsScriptVM _vm;
        private readonly GlobalsLookupTable _nameLookup;

        public readonly int PresentProcess;
        private readonly int _presentPreprocess;
        private readonly int _presentText;

        private readonly int _rButtonDown;
        private readonly int _x360ButtonStartDown;
        private readonly int _x360ButtonADown;
        private readonly int _x360ButtonBDown;
        private readonly int _x360ButtonYDown;

        private readonly int _x360ButtonLeftDown;
        private readonly int _x360ButtonUpDown;
        private readonly int _x360ButtonRightDown;
        private readonly int _x360ButtonDownDown;

        private readonly int _x360ButtonLbDown;
        private readonly int _x360ButtonRbDown;
        private readonly int _backlogEnable;
        private readonly int _backlogRowMax;
        private readonly int _backlogPositionX;
        private readonly int _backlogPositionY;
        private readonly int _backlogRowInterval;
        private readonly int _backlogCharacterWidth;

        private readonly int _positionXTextIcon;
        private readonly int _positionYTextIcon;
        private readonly int _savePath;

        private readonly int _lastText;
        private readonly int _skip;
        private readonly int _textAuto;
        private readonly int _textAutoLock;
        private readonly int _menuLock;
        private readonly int _skipLock;
        private readonly int _backlogLock;

        public SystemVariableLookup(NsScriptVM vm)
        {
            _vm = vm;
            _nameLookup = vm.GlobalsLookup;

            _savePath = Lookup("SYSTEM_save_path");

            _presentPreprocess = Lookup("SYSTEM_present_preprocess");
            _presentText = Lookup("SYSTEM_present_text");
            PresentProcess = Lookup("SYSTEM_present_process");
            _rButtonDown = Lookup("SYSTEM_r_button_down");
            _x360ButtonStartDown = Lookup("SYSTEM_XBOX360_button_start_down");
            _x360ButtonADown = Lookup("SYSTEM_XBOX360_button_a_down");
            _x360ButtonBDown = Lookup("SYSTEM_XBOX360_button_b_down");
            _x360ButtonYDown = Lookup("SYSTEM_XBOX360_button_y_down");
            _x360ButtonLeftDown = Lookup("SYSTEM_XBOX360_button_left_down");
            _x360ButtonUpDown = Lookup("SYSTEM_XBOX360_button_up_down");
            _x360ButtonRightDown = Lookup("SYSTEM_XBOX360_button_right_down");
            _x360ButtonDownDown = Lookup("SYSTEM_XBOX360_button_down_down");
            _x360ButtonLbDown = Lookup("SYSTEM_XBOX360_button_lb_down");
            _x360ButtonRbDown = Lookup("SYSTEM_XBOX360_button_rb_down");

            _backlogEnable = Lookup("SYSTEM_backlog_enable");
            _backlogRowMax = Lookup("SYSTEM_backlog_row_max");
            _backlogPositionX = Lookup("SYSTEM_backlog_position_x");
            _backlogPositionY = Lookup("SYSTEM_backlog_position_y");
            _backlogRowInterval = Lookup("SYSTEM_backlog_row_interval");
            _backlogCharacterWidth = Lookup("SYSTEM_backlog_character_width");

            _positionXTextIcon = Lookup("SYSTEM_position_x_text_icon");
            _positionYTextIcon = Lookup("SYSTEM_position_y_text_icon");
            _lastText = Lookup("SYSTEM_last_text");
            _skip = Lookup("SYSTEM_skip");
            _textAuto = Lookup("SYSTEM_text_auto");
            _textAutoLock = Lookup("SYSTEM_text_auto_lock");

            _menuLock = Lookup("SYSTEM_menu_lock");
            _skipLock = Lookup("SYSTEM_skip_lock");
            _backlogLock = Lookup("SYSTEM_backlog_lock");
        }

        private int Lookup(string name)
        {
            if (!_nameLookup.TryLookupSystemVariable(name, out int index))
            {
                _nameLookup.TryLookupSystemFlag(name, out index);
            }
            return index;
        }

        public ref ConstantValue CurrentSubroutineName => ref Var(PresentProcess);
        public ref ConstantValue CurrentDialogueBox => ref Var(_presentPreprocess);
        public ref ConstantValue CurrentDialogueBlock => ref Var(_presentText);
        public ref ConstantValue RightButtonDown => ref Var(_rButtonDown);

        public ref ConstantValue X360StartButtonDown => ref Var(_x360ButtonStartDown);
        public ref ConstantValue X360AButtonDown => ref Var(_x360ButtonADown);
        public ref ConstantValue X360BButtonDown => ref Var(_x360ButtonBDown);
        public ref ConstantValue X360YButtonDown => ref Var(_x360ButtonYDown);

        public ref ConstantValue X360LeftButtonDown => ref Var(_x360ButtonLeftDown);
        public ref ConstantValue X360UpButtonDown => ref Var(_x360ButtonUpDown);
        public ref ConstantValue X360RightButtonDown => ref Var(_x360ButtonRightDown);
        public ref ConstantValue X360DownButtonDown => ref Var(_x360ButtonDownDown);

        public ref ConstantValue X360LbButtonDown => ref Var(_x360ButtonLbDown);
        public ref ConstantValue X360RbButtonDown => ref Var(_x360ButtonRbDown);

        private ref ConstantValue Var(int index) => ref _vm.GetVariable(index);
        private ref ConstantValue Flag(int index) => ref _vm.GetFlag(index);

        public ref ConstantValue BacklogEnable => ref _vm.GetVariable(_backlogEnable);
        public ref ConstantValue BacklogRowMax => ref _vm.GetVariable(_backlogRowMax);
        public ref ConstantValue BacklogRowInterval => ref _vm.GetVariable(_backlogRowInterval);
        public ref ConstantValue BacklogPositionX => ref _vm.GetVariable(_backlogPositionX);
        public ref ConstantValue BacklogPositionY => ref _vm.GetVariable(_backlogPositionY);
        public ref ConstantValue BacklogCharacterWidth => ref _vm.GetVariable(_backlogCharacterWidth);

        public ref ConstantValue PositionXTextIcon => ref _vm.GetVariable(_positionXTextIcon);
        public ref ConstantValue PositionYTextIcon => ref _vm.GetVariable(_positionYTextIcon);

        public ref ConstantValue SavePath => ref Flag(_savePath);

        public ref ConstantValue LastText => ref Var(_lastText);

        public ref ConstantValue Skip => ref Var(_skip);
        public ref ConstantValue TextAuto => ref Var(_textAuto);
        public ref ConstantValue TextAutoLock => ref Var(_textAutoLock);

        public ref ConstantValue MenuLock => ref Var(_menuLock);
        public ref ConstantValue SkipLock => ref Var(_skipLock);
        public ref ConstantValue BacklogLock => ref Var(_backlogLock);
    }
}
