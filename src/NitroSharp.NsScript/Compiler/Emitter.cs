using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Utilities;

namespace NitroSharp.NsScript.Compiler
{
    internal ref struct Emitter
    {
        private readonly NsxModuleBuilder _module;
        private readonly SubroutineSymbol _subroutine;
        private readonly Checker _checker;
        private readonly Compilation _compilation;
        private BufferWriter _code;
        private int _textId;
        private ValueStack<BreakScope> _breakScopes;
        private bool _supressConstantLookup;
        private bool _clearPage;

        private Emitter(NsxModuleBuilder moduleBuilder, SubroutineSymbol subroutine)
        {
            _module = moduleBuilder;
            _subroutine = subroutine;
            _checker = new Checker(subroutine, moduleBuilder.Diagnostics);
            _compilation = moduleBuilder.Compilation;
            _breakScopes = new ValueStack<BreakScope>(initialCapacity: 4);
            _code = default;
            _textId = 0;
            _supressConstantLookup = false;
            _clearPage = true;

            if (subroutine is FunctionSymbol function && function.Parameters.Length > 0)
            {
                foreach (ParameterSymbol p in function.Parameters)
                {
                    _ = GetVariableToken(p.Name);
                }
            }
        }

        private readonly struct JumpPlaceholder
        {
            public readonly int InstructionPos;

            public JumpPlaceholder(int instrPos)
                => InstructionPos = instrPos;

            public int OffsetPos => InstructionPos + 1;
        }

        private struct BreakScope
        {
            private Queue<JumpPlaceholder>? _breakPlaceholders;

            public Queue<JumpPlaceholder> BreakPlaceholders
                => _breakPlaceholders ??= new Queue<JumpPlaceholder>();
        }

        private void EmitOpcode(Opcode opcode)
            => _code.WriteByte((byte)opcode);

        private ushort GetVariableToken(string name)
            => _compilation.GetVariableToken(name);

        private ushort GetFlagToken(string name)
            => _compilation.GetFlagToken(name);

        public static void CompileSubroutine(
            NsxModuleBuilder moduleBuilder, SubroutineSymbol subroutine,
            ref BufferWriter codeBuffer, List<int> dialogueBlockOffsets)
        {
            Debug.Assert(dialogueBlockOffsets.Count == 0);
            int start = codeBuffer.Position;
            var emitter = new Emitter(moduleBuilder, subroutine);
            emitter._code = codeBuffer;
            emitter.EmitStatement(subroutine.Declaration.Body);
            emitter.EmitOpcode(Opcode.Return);

            SubroutineDeclaration decl = subroutine.Declaration;
            ImmutableArray<DialogueBlock> dialogueBlocks = decl.DialogueBlocks;
            foreach (DialogueBlock dialogueBlock in dialogueBlocks)
            {
                dialogueBlockOffsets.Add(emitter._code.Position - start);
                emitter.EmitDialogueBlock(dialogueBlock);
            }
            codeBuffer = emitter._code;
        }

        private void EmitUnary(UnaryOperatorKind opKind)
        {
            switch (opKind)
            {
                case UnaryOperatorKind.Not:
                    EmitOpcode(Opcode.Invert);
                    break;
                case UnaryOperatorKind.Minus:
                    EmitOpcode(Opcode.Neg);
                    break;
                case UnaryOperatorKind.Delta:
                    EmitOpcode(Opcode.Delta);
                    break;
            }
        }

        private void EmitBinary(BinaryOperatorKind opKind)
        {
            switch (opKind)
            {
                case BinaryOperatorKind.Equals:
                    EmitOpcode(Opcode.Equal);
                    break;
                case BinaryOperatorKind.NotEquals:
                    EmitOpcode(Opcode.NotEqual);
                    break;
                default:
                    EmitOpcode(Opcode.Binary);
                    _code.WriteByte((byte)opKind);
                    break;
            }
        }

        private void EmitExpression(Expression expression)
        {
            switch (expression.Kind)
            {
                case SyntaxNodeKind.LiteralExpression:
                    EmitLiteral((LiteralExpression)expression);
                    break;
                case SyntaxNodeKind.NameExpression:
                    EmitNameExpression((NameExpression)expression);
                    break;
                case SyntaxNodeKind.UnaryExpression:
                    EmitUnaryExpression((UnaryExpression)expression);
                    break;
                case SyntaxNodeKind.BinaryExpression:
                    EmitBinaryExpression((BinaryExpression)expression);
                    break;
                case SyntaxNodeKind.AssignmentExpression:
                    EmitAssignmentExpression((AssignmentExpression)expression);
                    break;
                case SyntaxNodeKind.FunctionCallExpression:
                    EmitFunctionCall((FunctionCallExpression)expression);
                    break;
                case SyntaxNodeKind.BezierExpression:
                    EmitBezierExpression((BezierExpression)expression);
                    break;
            }
        }

        private void EmitLiteral(LiteralExpression literal)
        {
            ConstantValue val = literal.Value;
            if (val.IsString && !_supressConstantLookup)
            {
                string strVal = val.AsString()!;
                if (WellKnownSymbols.LookupBuiltInConstant(strVal) is BuiltInConstant constant)
                {
                    val = ConstantValue.BuiltInConstant(constant);
                }
            }
            EmitLoadImm(val);
        }

        private void EmitNameExpression(NameExpression expression)
        {
            LookupResult lookupResult = _checker.LookupNonInvocableSymbol(expression);
            switch (lookupResult.Variant)
            {
                case LookupResultVariant.BuiltInConstant:
                    EmitLoadImm(ConstantValue.BuiltInConstant(lookupResult.BuiltInConstant));
                    break;
                case LookupResultVariant.Variable:
                    Debug.Assert(lookupResult.Global is not null);
                    ushort varToken = GetVariableToken(lookupResult.Global);
                    EmitOpcode(Opcode.LoadVar);
                    _code.WriteUInt16LE(varToken);
                    break;
                case LookupResultVariant.Flag:
                    Debug.Assert(lookupResult.Global is not null);
                    ushort flagToken = GetFlagToken(lookupResult.Global);
                    EmitOpcode(Opcode.LoadFlag);
                    _code.WriteUInt16LE(flagToken);
                    break;
                case LookupResultVariant.Empty:
                    var literal = ConstantValue.String(expression.Name);
                    EmitLoadImm(literal);
                    break;
            }
        }

        private void EmitUnaryExpression(UnaryExpression expression)
        {
            EmitExpression(expression.Operand);
            EmitUnary(expression.OperatorKind.Value);
        }

        private void EmitBinaryExpression(BinaryExpression expression)
        {
            EmitExpression(expression.Right);
            EmitExpression(expression.Left);
            EmitBinary(expression.OperatorKind.Value);
        }

        private void EmitAssignmentExpression(AssignmentExpression assignmentExpr)
        {
            LookupResult target = _checker.ResolveAssignmentTarget(assignmentExpr.Target);
            if (target.IsEmpty) { return; }

            EmitExpression(assignmentExpr.Value);

            Debug.Assert(target.Variant is LookupResultVariant.Variable or LookupResultVariant.Flag);
            Debug.Assert(target.Global is not null);
            AssignmentOperatorKind opKind = assignmentExpr.OperatorKind.Value;

            Opcode loadOp;
            ushort token;
            if (target.Variant == LookupResultVariant.Variable)
            {
                token = GetVariableToken(target.Global);
                loadOp = Opcode.LoadVar;
            }
            else
            {
                token = GetFlagToken(target.Global);
                loadOp = Opcode.LoadFlag;
            }

            if (opKind != AssignmentOperatorKind.Assign)
            {
                EmitOpcode(loadOp);
                _code.WriteUInt16LE(token);
            }

            switch (opKind)
            {
                case AssignmentOperatorKind.Assign:
                    break;
                case AssignmentOperatorKind.AddAssign:
                    EmitBinary(BinaryOperatorKind.Add);
                    break;
                case AssignmentOperatorKind.SubtractAssign:
                    EmitBinary(BinaryOperatorKind.Subtract);
                    break;
                case AssignmentOperatorKind.MultiplyAssign:
                    EmitBinary(BinaryOperatorKind.Multiply);
                    break;
                case AssignmentOperatorKind.DivideAssign:
                    EmitBinary(BinaryOperatorKind.Divide);
                    break;
                case AssignmentOperatorKind.Increment:
                    EmitOpcode(Opcode.Inc);
                    break;
                case AssignmentOperatorKind.Decrement:
                    EmitOpcode(Opcode.Dec);
                    break;
            }

            Opcode storeOp = target.Variant == LookupResultVariant.Variable
                ? Opcode.StoreVar
                : Opcode.StoreFlag;
            EmitStore(storeOp, token);
        }

        private void EmitFunctionCall(FunctionCallExpression callExpression)
        {
            LookupResult lookupResult = _checker.LookupFunction(callExpression.TargetName);
            if (lookupResult.IsEmpty) { return; }
            bool isBuiltIn = lookupResult.Variant == LookupResultVariant.BuiltInFunction;
            ImmutableArray<Expression> arguments = callExpression.Arguments;
            bool supressConstantLookup = _supressConstantLookup;

            if (!isBuiltIn)
            {
                Debug.Assert(lookupResult.Subroutine is not null);
                var target = (FunctionSymbol)lookupResult.Subroutine;
                int count = Math.Min(arguments.Length, target.Parameters.Length);
                for (int i = 0; i < count; i++)
                {
                    _supressConstantLookup = true;
                    EmitExpression(arguments[i]);
                    EmitStore(Opcode.StoreVar, GetVariableToken( target.Parameters[i].Name));
                    _supressConstantLookup = supressConstantLookup;
                }
            }
            else
            {
                // Assumption: the first argument is never a built-in constant
                // Even if it looks like one (e.g. "Black"), it should be treated
                // as a string literal, not as a built-in constant.
                // Reasoning: "Black" and "White" are sometimes used as entity names.
                // Update: also make an exception for regular function calls (see code above).
                for (int i = 0; i < arguments.Length; i++)
                {
                    _supressConstantLookup = i == 0;
                    EmitExpression(arguments[i]);
                    _supressConstantLookup = supressConstantLookup;
                }
            }

            if (isBuiltIn)
            {
                EmitOpcode(Opcode.Dispatch);
                _code.WriteByte((byte)lookupResult.BuiltInFunction);
                _code.WriteByte((byte)callExpression.Arguments.Length);
            }
            else
            {
                Debug.Assert(lookupResult.Subroutine is not null);
                var function = (FunctionSymbol)lookupResult.Subroutine;
                if (ReferenceEquals(function.DeclaringSourceFile, _subroutine.DeclaringSourceFile))
                {
                    EmitOpcode(Opcode.Call);
                    _code.WriteUInt16LE(_module.GetSubroutineToken(function));
                    _code.WriteByte((byte)callExpression.Arguments.Length);
                }
                else
                {
                    EmitOpcode(Opcode.CallFar);
                    SourceFileSymbol externalSourceFile = function.DeclaringSourceFile;
                    NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
                    _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
                    _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(function));
                    _code.WriteByte((byte)callExpression.Arguments.Length);
                }
            }
        }

        private void EmitBezierExpression(BezierExpression expr)
        {
            if (_checker.ParseBezierCurve(expr, out ImmutableArray<CompileTimeBezierSegment> segments))
            {
                EmitOpcode(Opcode.BezierStart);
                foreach (CompileTimeBezierSegment seg in segments.Reverse())
                {
                    seg.Points.Reverse();
                    foreach (BezierControlPoint cp in seg.Points)
                    {
                        EmitExpression(cp.Y);
                        EmitExpression(cp.X);
                    }
                    EmitOpcode(Opcode.BezierEndSeg);
                }
                EmitOpcode(Opcode.BezierEnd);
            }
        }

        private void EmitCallChapter(CallChapterStatement statement)
        {
            if (_checker.ResolveCallChapterTarget(statement) is ChapterSymbol chapter)
            {
                EmitCall(Opcode.CallScene, chapter);
            }
        }

        private void EmitCallScene(CallSceneStatement statement)
        {
            if (_checker.ResolveCallSceneTarget(statement) is SceneSymbol scene)
            {
                EmitCall(Opcode.CallScene, scene);
            }
        }

        private void EmitCall(Opcode opcode, SubroutineSymbol target)
        {
            EmitOpcode(opcode);
            SourceFileSymbol externalSourceFile = target.DeclaringSourceFile;
            NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
            _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
            _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(target));
        }

        private void EmitStatement(Statement statement)
        {
            switch (statement.Kind)
            {
                case SyntaxNodeKind.Block:
                    EmitBlock((Block)statement);
                    break;
                case SyntaxNodeKind.ExpressionStatement:
                    EmitExpression(((ExpressionStatement)statement).Expression);
                    break;
                case SyntaxNodeKind.IfStatement:
                    EmitIfStatement((IfStatement)statement);
                    break;
                case SyntaxNodeKind.BreakStatement:
                    EmitBreakStatement((BreakStatement)statement);
                    break;
                case SyntaxNodeKind.WhileStatement:
                    EmitWhileStatement((WhileStatement)statement);
                    break;
                case SyntaxNodeKind.ReturnStatement:
                    EmitOpcode(Opcode.Return);
                    break;
                case SyntaxNodeKind.CallChapterStatement:
                    EmitCallChapter((CallChapterStatement)statement);
                    break;
                case SyntaxNodeKind.CallSceneStatement:
                    EmitCallScene((CallSceneStatement)statement);
                    break;
                case SyntaxNodeKind.SelectStatement:
                    EmitSelect((SelectStatement)statement);
                    break;
                case SyntaxNodeKind.SelectSection:
                    EmitSelectSection((SelectSection)statement);
                    break;
                case SyntaxNodeKind.DialogueBlock:
                    EmitOpcode(Opcode.ActivateBlock);
                    _code.WriteUInt16LE((ushort)_textId++);
                    break;
                case SyntaxNodeKind.PXmlString:
                    var text = (PXmlString)statement;
                    if (_clearPage)
                    {
                        EmitOpcode(Opcode.ClearPage);
                        _clearPage = false;
                    }
                    EmitOpcode(Opcode.AppendDialogue);
                    _code.WriteUInt16LE(_module.GetStringToken(text.Text));
                    break;
                case SyntaxNodeKind.PXmlLineSeparator:
                    EmitOpcode(Opcode.LineEnd);
                    _clearPage = true;
                    break;
            }
        }

        private void EmitBlock(Block block)
        {
            foreach (Statement statement in block.Statements)
            {
                EmitStatement(statement);
            }
        }

        private void EmitIfStatement(IfStatement ifStmt)
        {
            EmitExpression(ifStmt.Condition);
            if (ifStmt.IfFalseStatement == null)
            {
                // if (<condition>)
                //      <consequence>
                //
                // ---->
                //
                // <condition>
                // JumpIfFalse exit
                // <consequence>
                // exit:

                JumpPlaceholder exitJump = EmitJump(Opcode.JumpIfFalse);
                EmitStatement(ifStmt.IfTrueStatement);
                PatchJump(exitJump, _code.Position);
            }
            else
            {
                // if <condition>
                //      <consequence>
                // else
                //      <alternative>
                //
                // ---->
                //
                // <condition>
                // JumpIfFalse alternative
                // <consequence>
                // Jump exit
                // <alternative>
                // exit:

                JumpPlaceholder altJump = EmitJump(Opcode.JumpIfFalse);
                EmitStatement(ifStmt.IfTrueStatement);
                JumpPlaceholder exitJump = EmitJump(Opcode.Jump);
                int alternativePos = _code.Position;
                EmitStatement(ifStmt.IfFalseStatement);
                PatchJump(exitJump, _code.Position);
                PatchJump(altJump, alternativePos);
            }
        }

        private void EmitWhileStatement(WhileStatement whileStmt)
        {
            // while (<condition>)
            //     <body>
            //
            // ---->
            //
            // <condition>
            // JumpIfFalse exit
            // <body>
            // Jump <condition>
            // exit:

            int loopStart = _code.Position;
            EmitExpression(whileStmt.Condition);
            JumpPlaceholder exitJump = EmitJump(Opcode.JumpIfFalse);
            BreakScope bodyScope = EmitLoopBody(whileStmt.Body);
            EmitJump(Opcode.Jump, loopStart);
            PatchJump(exitJump, _code.Position);
            PatchBreaks(ref bodyScope, _code.Position);
        }

        private void EmitSelect(SelectStatement selectStmt)
        {
            int loopStart = _code.Position;
            EmitOpcode(Opcode.SelectLoopStart);
            BreakScope bodyScope = EmitLoopBody(selectStmt.Body);
            EmitOpcode(Opcode.SelectLoopEnd);
            EmitJump(Opcode.JumpIfFalse, loopStart);
            PatchBreaks(ref bodyScope, _code.Position);
            EmitOpcode(Opcode.SelectEnd);
        }

        private void EmitSelectSection(SelectSection section)
        {
            EmitOpcode(Opcode.IsPressed);
            _code.WriteUInt16LE(_module.GetStringToken(section.Label.Value));
            JumpPlaceholder jmp = EmitJump(Opcode.JumpIfFalse);
            EmitStatement(section.Body);
            PatchJump(jmp, _code.Position);
        }

        private BreakScope EmitLoopBody(Statement body)
        {
            _breakScopes.Push(new BreakScope());
            EmitStatement(body);
            return _breakScopes.Pop();
        }

        private void EmitBreakStatement(BreakStatement breakStmt)
        {
            if (_breakScopes.Count == 0)
            {
                _checker.Report(breakStmt, DiagnosticId.MisplacedBreak);
                return;
            }

            EmitBreakPlaceholder();
        }

        private void EmitBreakPlaceholder()
        {
            ref BreakScope scope = ref _breakScopes.Peek();
            scope.BreakPlaceholders.Enqueue(EmitJump(Opcode.Jump));
        }

        private void PatchBreaks(ref BreakScope scope, int destination)
        {
            while (scope.BreakPlaceholders.TryDequeue(out JumpPlaceholder jump))
            {
                PatchJump(jump, destination);
            }
        }

        private void EmitJump(Opcode opcode, int dst)
        {
            AssertJumpInstr(opcode);
            int pos = _code.Position;
            EmitOpcode(opcode);
            _code.WriteInt16LE((short)(dst - pos));
        }

        private JumpPlaceholder EmitJump(Opcode opcode)
        {
            AssertJumpInstr(opcode);
            int pos = _code.Position;
            EmitOpcode(opcode);
            _code.WriteInt16LE(0);
            return new JumpPlaceholder(pos);
        }

        private static void AssertJumpInstr(Opcode opcode)
        {
            Debug.Assert(opcode == Opcode.Jump
                || opcode == Opcode.JumpIfFalse
                || opcode == Opcode.JumpIfTrue
            );
        }

        private void PatchJump(JumpPlaceholder jumpPlaceholder, int dst)
        {
            int oldPos = _code.Position;
            _code.Position = jumpPlaceholder.OffsetPos;
            _code.WriteInt16LE((short)(dst - jumpPlaceholder.InstructionPos));
            _code.Position = oldPos;
        }

        private void EmitStore(Opcode opcode, ushort tk)
        {
            EmitOpcode(opcode);
            _code.WriteUInt16LE(tk);
        }

        private void EmitLoadImm(in ConstantValue value)
        {
            switch (value.Type)
            {
                case BuiltInType.Numeric:
                    float num = value.AsNumber()!.Value;
                    switch (num)
                    {
                        case 0:
                            EmitOpcode(Opcode.LoadImm0);
                            break;
                        case 1:
                            EmitOpcode(Opcode.LoadImm1);
                            break;
                        default:
                            EmitOpcode(Opcode.LoadImm);
                            _code.WriteByte((byte)value.Type);
                            _code.WriteSingle(num);
                            break;
                    }
                    break;
                case BuiltInType.DeltaNumeric:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteSingle(value.AsDeltaNumber()!.Value);
                    break;
                case BuiltInType.Boolean:
                    Opcode opcode = value.AsBool()!.Value
                        ? Opcode.LoadImmTrue
                        : Opcode.LoadImmFalse;
                    EmitOpcode(opcode);
                    break;
                case BuiltInType.String:
                    string str = value.AsString()!;
                    if (string.IsNullOrEmpty(str))
                    {
                        EmitOpcode(Opcode.LoadImmEmptyStr);
                    }
                    else
                    {
                        EmitOpcode(Opcode.LoadImm);
                        _code.WriteByte((byte)value.Type);
                        ushort token = _module.GetStringToken(str);
                        _code.WriteUInt16LE(token);
                    }
                    break;
                case BuiltInType.BuiltInConstant:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteByte((byte)value.AsBuiltInConstant()!.Value);
                    break;
                case BuiltInType.Null:
                    EmitOpcode(Opcode.LoadImmNull);
                    break;
            }
        }

        private void EmitDialogueBlock(DialogueBlock dialogueBlock)
        {
            foreach (Statement statement in dialogueBlock.Parts)
            {
                EmitStatement(statement);
            }
            EmitOpcode(Opcode.Return);
        }
    }
}
