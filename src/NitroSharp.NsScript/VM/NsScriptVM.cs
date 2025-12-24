using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NitroSharp.NsScript.Primitives;
using NitroSharp.NsScript.Utilities;
using ZeroLog;

namespace NitroSharp.NsScript.VM;

[Persistable]
public readonly partial struct GlobalsDump
{
    internal (string, ConstantValue)[] Globals { get; init; }
}

public sealed class NsScriptVM
{
    private readonly Log _log;
    private readonly NsxModuleLocator _moduleLocator;
    private readonly Dictionary<string, NsxModule> _loadedModules;
    private readonly BuiltInFunctionDispatcher _builtInCallDispatcher;
    private readonly ConstantValue[] _variables;
    private readonly ConstantValue[] _flags;
    private readonly Stack<CubicBezierSegment> _bezierSegmentStack;

    public NsScriptVM(NsxModuleLocator moduleLocator, Stream globalsLookupTableStream)
    {
        _log = LogManager.GetLogger("VM");
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
        FrozenDictionary<string, int> lookup)
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
        FrozenDictionary<string, int> lookup,
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
        var frame = CreateEntryPointCallFrame(module, (ushort)index);
        return new NsScriptThreadState(frame);
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

    public enum TickResult
    {
        Ok,
        Yield
    }

    public TickResult Tick(ref NsScriptThreadState thread, BuiltInFunctions builtins)
    {
        try
        {
            return TickCore(ref thread, builtins);
        }
        catch (Exception e)
        {
            ReportException(thread, e);
            throw;
        }
    }

    private TickResult TickCore(ref NsScriptThreadState thread, BuiltInFunctions builtins)
    {
        if (thread.CallFrameStack.Count == 0)
        {
            return TickResult.Yield;
        }

        ref CallFrame frame = ref thread.CurrentFrame;
        NsxModule thisModule = frame.Module;
        builtins._vm = this;
        builtins.CurrentModule = thisModule;
        var program = new BytecodeStream(thisModule.Code, frame.ProgramCounter);
        ref ValueStack<ConstantValue> evalStack = ref thread.EvalStack;
        while (true)
        {
            frame.ProgramCounter = program.Position;
            Opcode opcode = program.NextOpcode();
            if (opcode is >= Opcode.LoadImm0 and <= Opcode.LoadFlag)
            {
                handleLoadOp(opcode, ref program, ref thread);
                continue;
            }

            switch (opcode)
            {
                case Opcode.StoreVar:
                    int index = program.DecodeToken();
                    GetVariable(index) = evalStack.Pop();
                    break;
                case Opcode.StoreFlag:
                    index = program.DecodeToken();
                    GetFlag(index) = evalStack.Pop();
                    break;
                case Opcode.Binary:
                    var opKind = (BinaryOperatorKind)program.ReadByte();
                    ConstantValue op1 = evalStack.Pop();
                    ConstantValue op2 = evalStack.Pop();
                    evalStack.Push(BinOp(op1, opKind, op2));
                    break;
                case Opcode.Equal:
                    op1 = evalStack.Pop();
                    op2 = evalStack.Pop();
                    evalStack.Push(op1 == op2);
                    break;
                case Opcode.NotEqual:
                    op1 = evalStack.Pop();
                    op2 = evalStack.Pop();
                    evalStack.Push(op1 != op2);
                    break;
                case Opcode.Neg:
                    ref ConstantValue val = ref evalStack.Peek();
                    val = val.Type switch
                    {
                        BuiltInType.Numeric => ConstantValue.Number(-val.AsNumber()!.Value),
                        // TODO: runtime error
                        _ => throw ThrowHelper.Unreachable()
                    };
                    break;
                case Opcode.Inc:
                    val = ref evalStack.Peek();
                    Debug.Assert(val.Type == BuiltInType.Numeric); // TODO: runtime error
                    val = ConstantValue.Number(val.AsNumber()!.Value + 1);
                    break;
                case Opcode.Dec:
                    val = ref evalStack.Peek();
                    Debug.Assert(val.Type == BuiltInType.Numeric); // TODO: runtime error
                    val = ConstantValue.Number(val.AsNumber()!.Value - 1);
                    break;
                case Opcode.Delta:
                    val = ref evalStack.Peek();
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
                    val = ref evalStack.Peek();
                    Debug.Assert(val.AsBool() is not null);
                    val = ConstantValue.Boolean(!val.AsBool()!.Value);
                    break;
                case Opcode.Pop:
                    evalStack.Pop();
                    break;
                case Opcode.Call:
                    ushort subroutineToken = program.DecodeToken();
                    frame.ProgramCounter = program.Position;
                    CallFrame newFrame = CreateEntryPointCallFrame(frame.Module, subroutineToken);
                    thread.CallFrameStack.Push(newFrame);
                    if (_log.IsDebugEnabled)
                    {
                        logCall(newFrame);
                    }
                    return TickResult.Ok;
                case Opcode.CallFar:
                case Opcode.CallChapter:
                    newFrame = externalCall(ref program);
                    frame.ProgramCounter = program.Position;
                    thread.CallFrameStack.Push(newFrame);
                    return TickResult.Ok;
                case Opcode.CallScene:
                    newFrame = externalCall(ref program);
                    frame.ProgramCounter = program.Position;
                    var newThread = new NsScriptThreadState(newFrame);
                    // TODO: CallScene not actually implemented
                    //Join(thread, newThread);
                    //ResumeThread(newThread);
                    return TickResult.Ok;
                case Opcode.Jump:
                    int @base = program.Position - 1;
                    int offset = program.DecodeOffset();
                    program.Position = @base + offset;
                    break;
                case Opcode.JumpIfTrue:
                    @base = program.Position - 1;
                    ConstantValue condition = evalStack.Pop();
                    offset = program.DecodeOffset();
                    Debug.Assert(condition.Type == BuiltInType.Boolean);
                    if (condition.AsBool()!.Value)
                    {
                        program.Position = @base + offset;
                    }
                    break;
                case Opcode.JumpIfFalse:
                    @base = program.Position - 1;
                    condition = evalStack.Pop();
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

                    var seg = new CubicBezierSegment(
                        popPoint(ref evalStack),
                        popPoint(ref evalStack),
                        popPoint(ref evalStack),
                        popPoint(ref evalStack)
                    );
                    _bezierSegmentStack.Push(seg);
                    break;

                    static BezierControlPoint popPoint(ref ValueStack<ConstantValue> stack)
                    {
                        var x = NsCoordinate.FromValue(stack.Pop());
                        var y = NsCoordinate.FromValue(stack.Pop());
                        return new BezierControlPoint(x, y);
                    }
                case Opcode.BezierEnd:
                    var curve = new CompositeBezier(_bezierSegmentStack.ToImmutableArray());
                    evalStack.Push(ConstantValue.BezierCurve(curve));
                    break;
                case Opcode.Dispatch:
                    dispatchBuiltIn(ref program, ref thread);
                    frame.ProgramCounter = program.Position;
                    return TickResult.Ok;

                case Opcode.ActivateBlock:
                    ushort blockId = program.DecodeToken();
                    (string box, string textName) = frame.RuntimeInfo.DialogueBlockInfos[blockId];
                    SystemVariables.CurrentDialogueBox = ConstantValue.String(box);
                    SystemVariables.CurrentDialogueBlock = ConstantValue.String($"@{textName}");
                    break;
                case Opcode.SelectLoopStart:
                    thread.SelectResult = false;
                    break;
                case Opcode.IsPressed:
                    string choice = thisModule.GetString(program.DecodeToken());
                    bool pressed = builtins.HandleInputEvents(EntityQuery.Parse(choice));
                    evalStack.Push(ConstantValue.Boolean(pressed));
                    thread.SelectResult |= pressed;
                    break;
                case Opcode.SelectLoopEnd:
                    evalStack.Push(ConstantValue.Boolean(thread.SelectResult));
                    frame.ProgramCounter = program.Position;
                    return TickResult.Yield;
                case Opcode.SelectEnd:
                    builtins.SelectEnd();
                    break;
            }
        }

        void handleLoadOp(Opcode opcode, ref BytecodeStream program, ref NsScriptThreadState thread)
        {
            ushort varToken = ushort.MaxValue;
            ushort flagToken;
            ConstantValue? imm = opcode switch
            {
                Opcode.LoadImm => readConst(ref program),
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

            if (varToken != ushort.MaxValue && varToken == SystemVariables.PresentProcess)
            {
                string subName = thisModule.GetSubroutineName(thread.CurrentFrame.SubroutineIndex);
                imm = _variables[varToken] = ConstantValue.String(subName);
            }

            ConstantValue value = imm!.Value;
            thread.EvalStack.Push(ref value);
        }

        void dispatchBuiltIn(ref BytecodeStream program, ref NsScriptThreadState thread)
        {
            ref CallFrame frame = ref thread.CurrentFrame;
            ref ValueStack<ConstantValue> evalStack = ref thread.EvalStack;
            var func = (BuiltInFunction)program.ReadByte();
            int argCount = program.ReadByte();
            ReadOnlySpan<ConstantValue> args = evalStack.AsSpan(evalStack.Count - argCount, argCount);
            ConstantValue? result = null;
            switch (func)
            {
                default:
                    result = _builtInCallDispatcher.Dispatch(builtins, func, args);
                    break;

                case BuiltInFunction.log:
                    _log.Info(args[0].ConvertToString());
                    break;
                case BuiltInFunction.fail:
                    string subName = frame.RuntimeInfo.SubroutineName;
                    _log.Error($"{subName} + {program.Position - 1}: test failed.");
                    break;
                case BuiltInFunction.fail_msg:
                    subName = frame.RuntimeInfo.SubroutineName;
                    _log.Error($"{subName} + {program.Position - 1}: {args[0].ToString()}.");
                    break;
            }

            evalStack.Pop(argCount);
            evalStack.Push(result ?? ConstantValue.Null);
        }

        CallFrame externalCall(ref BytecodeStream program)
        {
            ushort importTableIndex = program.DecodeToken();
            ushort subroutineToken = program.DecodeToken();
            string externalModuleName = thisModule.Imports[importTableIndex];
            NsxModule externalModule = GetModule(externalModuleName);
            return CreateEntryPointCallFrame(externalModule, subroutineToken);
        }

        ConstantValue readConst(ref BytecodeStream stream)
        {
            Immediate imm = stream.DecodeImmediateValue();
            return imm.Type switch
            {
                BuiltInType.Numeric => ConstantValue.Number(imm.Numeric),
                BuiltInType.DeltaNumeric => ConstantValue.Delta(imm.Numeric),
                BuiltInType.BuiltInConstant => ConstantValue.BuiltInConstant(imm.Constant),
                BuiltInType.String => ConstantValue.String(thisModule.GetString(imm.StringToken)),
                _ => throw ThrowHelper.UnexpectedValueOf<BuiltInType>()
            };
        }

        void logCall(in CallFrame callFrame)
        {
            string moduleName = callFrame.Module.Name;
            string routineName = callFrame.RuntimeInfo.SubroutineName;
            _log.Debug($"{moduleName}::{routineName}");
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
            _ => throw ThrowHelper.UnexpectedValueOf<BinaryOperatorKind>()
        };
    }

    private static CallFrame CreateEntryPointCallFrame(NsxModule module, ushort subroutineIndex)
    {
        Subroutine subroutine = module.GetSubroutine(subroutineIndex);
        return new CallFrame(module, subroutineIndex, subroutine.BytecodeStart);
    }

    private void ReportException(in NsScriptThreadState thread, Exception exception)
    {
        LogMessage message = _log.ForLevel(LogLevel.Error)
            .Append("Runtime error: ")
            .Append(exception.Message)
            .Append("\n");

        message = WriteStackTrace(thread.CallFrameStack, message);
        message.Log();
    }

    private LogMessage WriteStackTrace(ValueStack<CallFrame> frames, LogMessage message)
    {
        using SourceMappingScope sourceMapping = _moduleLocator.BeginSourceMapping();
        for (int i = frames.Count - 1; i >= 0; i--)
        {
            ref CallFrame frame = ref frames[i];
            NsxModule module = frame.Module;
            string routineName = frame.RuntimeInfo.SubroutineName;
            CodeOffset codeOffset = i == frames.Count - 1
                ? frame.ProgramCounter
                : frame.ProgramCounter - 1;

            int? lineNumber = null;
            if (module.GetSourceSpan(codeOffset) is { } sourceSpan)
            {
                SourceText sourceText = sourceMapping.GetSourceText(module.Name);
                lineNumber = sourceText.GetLineNumberFromPosition(sourceSpan.Start) + 1;
                if (i == frames.Count - 1)
                {
                    ReadOnlySpan<char> context = sourceText.GetCharacterSpan(sourceSpan);
                    message = message.Append($"Context: {context.TrimEnd()}\n");
                }
            }

            message = message.Append($"    at {routineName} in {module.Name}.nss");
            if (lineNumber is not null)
            {
                message = message.Append($":line {lineNumber}");
            }

            message = message.Append('\n');
        }

        return message;
    }
}
