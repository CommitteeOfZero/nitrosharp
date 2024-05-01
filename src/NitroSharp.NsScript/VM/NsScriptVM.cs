using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.VM;

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

    public NsScriptVM(NsxModuleLocator moduleLocator, Stream globalsLookupTableStream)
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
    // public NsScriptProcess? CurrentProcess { get; private set; }
    //
    // public NsScriptProcess RestoreProcess(in NsScriptProcessDump dump)
    // {
    //     NsScriptProcess process = new(this, dump);
    //     _lastProcessId = Math.Max(_lastProcessId, process.Id);
    //     _lastThreadId = Math.Max(_lastThreadId, dump.Threads.Max(x => x.Id));
    //     return process;
    // }

    public GlobalsDump DumpVariables() => DumpGlobals(_variables, GlobalsLookup.Variables);
    public GlobalsDump DumpFlags() => DumpGlobals(_flags, GlobalsLookup.Flags);

    public void RestoreVariables(GlobalsDump dump)
    {
        RestoreGlobals(_variables, GlobalsLookup.Variables, dump);
    }

    public void RestoreFlags(GlobalsDump dump)
    {
        RestoreGlobals(_flags, GlobalsLookup.Flags, dump);
    }

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

    // public NsScriptProcess CreateProcess(string moduleName, string symbol)
    // {
    //     uint pid = ++_lastProcessId;
    //     NsScriptThread? mainThread = CreateThread(moduleName, symbol);
    //     if (mainThread is null)
    //     {
    //         throw new ArgumentException($"Symbol '{symbol}' not found in module '{moduleName}'");
    //     }
    //     return new NsScriptProcess(this, pid, mainThread);
    // }
    //
    // public NsScriptProcessState? CreateProcess(string moduleName, string symbol)
    // {
    //     uint pid = ++_lastProcessId;
    //     NsScriptThreadState? mainThread = CreateThread(moduleName, symbol);
    //     if (mainThread is null)
    //     {
    //         throw new ArgumentException($"Symbol '{symbol}' not found in module '{moduleName}'");
    //     }
    //
    //     return new NsScriptProcessState(pid);
    // }

    // public NsScriptThread? CreateThread(NsScriptProcess process, string symbol, bool start = false)
    //     => CreateThread(process, process.CurrentThread!.CurrentFrame.Module.Name, symbol, start);

    // private NsScriptThread? CreateThread(string moduleName, string symbol)
    // {
    //     NsxModule? module = GetModule(moduleName);
    //     if (!module.TryLookupSubroutineIndex(symbol, out int index))
    //     {
    //         module = module.Imports
    //             .Select(GetModule)
    //             .FirstOrDefault(import => import.TryLookupSubroutineIndex(symbol, out index));
    //     }
    //
    //     if (module is null) { return null; }
    //     var frame = new CallFrame(module, (ushort)index, 0);
    //     return new NsScriptThread(++_lastThreadId, ref frame);
    // }

    public NsScriptThreadState? CreateThread(string moduleName, string symbol)
    {
        NsxModule? module = GetModule(moduleName);
        if (!module.TryLookupSubroutineIndex(symbol, out int index))
        {
            module = module.Imports
                .Select(GetModule)
                .FirstOrDefault(import => import.TryLookupSubroutineIndex(symbol, out index));
        }

        if (module is null) { return null; }
        var frame = new CallFrame(module, (ushort)index, 0);
        return new NsScriptThreadState(frame);
    }

    // public NsScriptThread ActivateDialogueBlock(in DialogueBlockToken blockToken)
    //     => ActivateDialogueBlock(CurrentProcess!, blockToken);
    //
    // private NsScriptThread ActivateDialogueBlock(
    //     NsScriptProcess process,
    //     in DialogueBlockToken blockToken)
    // {
    //     var frame = new CallFrame(
    //         blockToken.Module,
    //         (ushort)blockToken.SubroutineIndex,
    //         pc: blockToken.Offset
    //     );
    //     NsScriptThread thread = CreateThread(ref frame, declaredId: process.CurrentThread!.Id);
    //     thread.DialoguePage = EntityPath.Parse("@" + blockToken.BlockName);
    //     return thread;
    // }

    // public void Run(
    //     NsScriptProcess process,
    //     BuiltInFunctions builtins,
    //     CancellationToken cancellationToken)
    // {
    //     builtins._vm = this;
    //     CurrentProcess = process;
    //     process.Tick();
    //
    //     while (process.IsRunning
    //            && (!process.Threads.IsEmpty || process.PendingThreadActions.Count > 0))
    //     {
    //         process.ProcessPendingThreadActions();
    //         uint nbActive = 0;
    //         foreach (NsScriptThread thread in process.Threads)
    //         {
    //             if (!process.IsRunning) { break; }
    //             if (thread is { IsActive: true, Yielded: false })
    //             {
    //                 process.CurrentThread = thread;
    //                 nbActive++;
    //                 TickResult tickResult = Tick(process, ref thread, builtins);
    //                 if (!process.IsRunning) { break; }
    //                 if (tickResult == TickResult.Yield)
    //                 {
    //                     thread.Yielded = true;
    //                     nbActive--;
    //                 }
    //                 else if (thread.DoneExecuting)
    //                 {
    //                     if (thread.WaitingThread is { DoneExecuting: false } waitingThread)
    //                     {
    //                         ResumeThread(waitingThread);
    //                         nbActive++;
    //                     }
    //                     TerminateThread(thread);
    //                     nbActive--;
    //                 }
    //             }
    //         }
    //
    //         if (nbActive == 0)
    //         {
    //             foreach (NsScriptThread thread in process.Threads)
    //             {
    //                 thread.Yielded = false;
    //             }
    //             break;
    //         }
    //     }
    // }

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

    public enum TickResult
    {
        Ok,
        Yield
    }

    public TickResult Tick(ref NsScriptThreadState thread, BuiltInFunctions builtins)
    {
        if (thread.CallFrameStack.Count == 0)
        {
            return TickResult.Ok;
        }

        ref CallFrame frame = ref thread.CurrentFrame;
        NsxModule thisModule = frame.Module;
        builtins._vm = this;
        builtins.CurrentModule = thisModule;
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
                    if (val.AsNumber() is { } num)
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
                    Debug.Assert(val.AsBool() is not null);
                    val = ConstantValue.Boolean(!val.AsBool()!.Value);
                    break;
                case Opcode.Call:
                    ushort subroutineToken = program.DecodeToken();
                    frame.ProgramCounter = program.Position;
                    var newFrame = new CallFrame(frame.Module, subroutineToken, 0);
                    thread.CallFrameStack.Push(newFrame);
                    return TickResult.Ok;
                case Opcode.CallFar:
                    newFrame = externalCall(ref program);
                    thread.CallFrameStack.Push(newFrame);
                    frame.ProgramCounter = program.Position;
                    return TickResult.Ok;
                case Opcode.CallScene:
                    newFrame = externalCall(ref program);
                    var newThread = new NsScriptThreadState(newFrame);

                    //Join(thread, newThread);
                    //ResumeThread(newThread);
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
                    ConstantValue? result = null;
                    switch (func)
                    {
                        default:
                            result = _builtInCallDispatcher.Dispatch(builtins, func, args);
                            stack.Pop(argCount);
                            break;

                        case BuiltInFunction.log:
                            ConstantValue arg = stack.Pop();
                            Console.WriteLine($"[VM]: {arg.ConvertToString()}");
                            break;
                        case BuiltInFunction.fail:
                            string subName = thisModule
                                .GetSubroutineRuntimeInfo(frame.SubroutineIndex).SubroutineName;
                            Console.WriteLine($"{subName} + {program.Position - 1}: test failed.");
                            break;
                        case BuiltInFunction.fail_msg:
                            ConstantValue message = stack.Pop();
                            subName = thisModule.GetSubroutineRuntimeInfo(frame.SubroutineIndex)
                                .SubroutineName;
                            Console.WriteLine($"{subName} + {program.Position - 1}: {message.ToString()}.");
                            break;
                    }
                    stack.Push(result ?? ConstantValue.Null);
                    frame.ProgramCounter = program.Position;
                    return TickResult.Ok;

                case Opcode.ActivateBlock:
                    ushort blockId = program.DecodeToken();
                    ref readonly var subroutineInfo = ref thisModule.GetSubroutineRuntimeInfo(frame.SubroutineIndex);
                    (string box, string textName) = subroutineInfo.DialogueBlockInfos[blockId];
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
                    bool pressed = builtins.HandleInputEvents(EntityPath.Parse(choice));
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
            _ => ThrowHelper.Unreachable<ConstantValue>()
        };
    }
}
