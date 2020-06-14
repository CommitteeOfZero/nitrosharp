using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly TokenMap<ParameterSymbol>? _parameters;
        private ValueStack<BreakScope> _breakScopes;
        private bool _supressConstantLookup;
        private bool _treatGlobalsAsOutVars;
        private List<string>? _outVariables;

        public Emitter(NsxModuleBuilder moduleBuilder, SubroutineSymbol subroutine)
        {
            _module = moduleBuilder;
            _subroutine = subroutine;
            _checker = new Checker(subroutine, moduleBuilder.Diagnostics);
            _compilation = moduleBuilder.Compilation;
            _parameters = null;
            _breakScopes = new ValueStack<BreakScope>(initialCapacity: 4);
            if (subroutine is FunctionSymbol function && function.Parameters.Length > 0)
            {
                ImmutableArray<ParameterSymbol> parameters = function.Parameters;
                _parameters = new TokenMap<ParameterSymbol>((uint)parameters.Length);
                foreach (ParameterSymbol param in parameters)
                {
                    _parameters.GetOrAddToken(param);
                }
            }
            _code = default;
            _textId = 0;
            _supressConstantLookup = false;
            _treatGlobalsAsOutVars = false;
            _outVariables = null;
        }

        private enum LoopKind
        {
            While,
            Select
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
            public readonly LoopKind LoopKind;
            private Queue<JumpPlaceholder>? _breakPlaceholders;

            public BreakScope(LoopKind loopKind)
            {
                LoopKind = loopKind;
                _breakPlaceholders = null;
            }

            public bool NoBreakPlaceholders
                => _breakPlaceholders == null;

            public Queue<JumpPlaceholder> BreakPlaceholders
                => _breakPlaceholders ??= new Queue<JumpPlaceholder>();
        }

        private void EmitOpcode(Opcode opcode)
            => _code.WriteByte((byte)opcode);

        private ushort GetGlobalVarToken(string variableName)
            => _compilation.GetGlobalVarToken(variableName);

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

            SubroutineDeclarationSyntax decl = subroutine.Declaration;
            ImmutableArray<DialogueBlockSyntax> dialogueBlocks = decl.DialogueBlocks;
            foreach (DialogueBlockSyntax dialogueBlock in dialogueBlocks)
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

        private void EmitExpression(ExpressionSyntax expression)
        {
            switch (expression.Kind)
            {
                case SyntaxNodeKind.LiteralExpression:
                    EmitLiteral((LiteralExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.NameExpression:
                    EmitNameExpression((NameExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.UnaryExpression:
                    EmitUnaryExpression((UnaryExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.BinaryExpression:
                    EmitBinaryExpression((BinaryExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.AssignmentExpression:
                    EmitAssignmentExpression((AssignmentExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.FunctionCallExpression:
                    EmitFunctionCall((FunctionCallExpressionSyntax)expression);
                    break;
                case SyntaxNodeKind.BezierExpression:
                    EmitBezierExpression((BezierExpressionSyntax)expression);
                    break;
            }
        }

        private void EmitLiteral(LiteralExpressionSyntax literal)
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

        private void EmitNameExpression(NameExpressionSyntax expression)
        {
            var spanned = new Spanned<string>(expression.Name, expression.Span);
            bool isVariable = expression.HasSigil;
            LookupResult lookupResult = _checker.LookupNonInvocableSymbol(spanned, isVariable);
            switch (lookupResult._variant)
            {
                case LookupResultVariant.BuiltInConstant:
                    EmitLoadImm(ConstantValue.BuiltInConstant(lookupResult.BuiltInConstant));
                    break;
                case LookupResultVariant.Parameter:
                    Debug.Assert(_parameters != null);
                    ushort slot = _parameters.GetOrAddToken(lookupResult.Parameter);
                    EmitLoadArg(slot);
                    break;
                case LookupResultVariant.GlobalVariable:
                    ushort varToken = GetGlobalVarToken(lookupResult.GlobalVariable);
                    EmitOpcode(Opcode.LoadVar);
                    _code.WriteUInt16LE(varToken);
                    if (_treatGlobalsAsOutVars)
                    {
                        _outVariables ??= new List<string>();
                        _outVariables.Add(lookupResult.GlobalVariable);
                        _compilation.BoundVariables.Add(lookupResult.GlobalVariable);
                    }
                    if (lookupResult.GlobalVariable.StartsWith("SYSTEM"))
                    {
                        _compilation.BoundVariables.Add(lookupResult.GlobalVariable);
                    }
                    break;
                case LookupResultVariant.Empty:
                    var literal = ConstantValue.String(expression.Name);
                    EmitLoadImm(literal);
                    break;
            }
        }

        private void EmitUnaryExpression(UnaryExpressionSyntax expression)
        {
            EmitExpression(expression.Operand);
            EmitUnary(expression.OperatorKind.Value);
        }

        private void EmitBinaryExpression(BinaryExpressionSyntax expression)
        {
            EmitExpression(expression.Left);
            EmitExpression(expression.Right);
            EmitBinary(expression.OperatorKind.Value);
        }

        enum VariableKind
        {
            Global,
            Parameter
        }

        private void EmitAssignmentExpression(AssignmentExpressionSyntax assignmentExpr)
        {
            LookupResult target = _checker.ResolveAssignmentTarget(assignmentExpr.Target);
            if (target.IsEmpty) { return; }

            EmitExpression(assignmentExpr.Value);

            ushort token;
            VariableKind variableKind;
            if (target._variant == LookupResultVariant.Parameter)
            {
                Debug.Assert(_parameters != null);
                token = _parameters.GetOrAddToken(target.Parameter);
                variableKind = VariableKind.Parameter;
            }
            else
            {
                Debug.Assert(target._variant == LookupResultVariant.GlobalVariable);
                token = GetGlobalVarToken(target.GlobalVariable);
                variableKind = VariableKind.Global;
                _compilation.BoundVariables.Add(target.GlobalVariable);
            }

            AssignmentOperatorKind opKind = assignmentExpr.OperatorKind.Value;
            if (opKind != AssignmentOperatorKind.Assign)
            {
                EmitLoadVariable(variableKind, token);
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

            EmitStoreOp(variableKind, token);
        }

        private void EmitFunctionCall(FunctionCallExpressionSyntax callExpression)
        {
            LookupResult lookupResult = _checker.LookupFunction(callExpression.TargetName);
            if (lookupResult.IsEmpty) { return; }
            bool isBuiltIn = lookupResult._variant == LookupResultVariant.BuiltInFunction;
            ImmutableArray<ExpressionSyntax> arguments = callExpression.Arguments;
            bool supressConstantLookup = _supressConstantLookup;

            if (isBuiltIn)
            {
                bool usesOutParams = lookupResult.BuiltInFunction switch
                {
                    BuiltInFunction.CursorPosition => true,
                    BuiltInFunction.Position => true,
                    BuiltInFunction.DateTime => true,
                    _ => false
                };
                if (usesOutParams)
                {
                    _treatGlobalsAsOutVars = true;
                }
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                // Assumption: the first argument is never a built-in constant
                // Even if it looks like one (e.g. "Black"), it should be treated
                // as a string literal, not as a built-in constant.
                // Reasoning: "Black" and "White" are sometimes used as entity names.
                // Update: also make an exception for regular function calls.
                _supressConstantLookup = i == 0 || !isBuiltIn;
                EmitExpression(arguments[i]);
                _supressConstantLookup = supressConstantLookup;
            }

            _treatGlobalsAsOutVars = false;

            if (isBuiltIn)
            {
                EmitOpcode(Opcode.Dispatch);
                _code.WriteByte((byte)lookupResult.BuiltInFunction);
                _code.WriteByte((byte)callExpression.Arguments.Length);
            }
            else
            {
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

        private void EmitBezierExpression(BezierExpressionSyntax expr)
        {
            if (_checker.ParseBezierCurve(expr, out ImmutableArray<CompileTimeBezierSegment> segments))
            {
                EmitOpcode(Opcode.BezierStart);
                foreach (CompileTimeBezierSegment seg in segments.Reverse())
                {
                    seg.Points.Reverse();
                    foreach (BezierControlPointSyntax cp in seg.Points)
                    {
                        EmitExpression(cp.Y);
                        EmitExpression(cp.X);
                    }
                    EmitOpcode(Opcode.BezierEndSeg);
                }
                EmitOpcode(Opcode.BezierEnd);
            }
        }

        private void EmitCallChapter(CallChapterStatementSyntax statement)
        {
            ChapterSymbol? chapter = _checker.ResolveCallChapterTarget(statement);
            if (chapter != null)
            {
                EmitCallFar(chapter);
            }
        }

        private void EmitCallScene(CallSceneStatementSyntax statement)
        {
            SceneSymbol? scene = _checker.ResolveCallSceneTarget(statement);
            if (scene != null)
            {
                EmitCallFar(scene);
            }
        }

        private void EmitCallFar(SubroutineSymbol subroutine)
        {
            EmitOpcode(Opcode.CallFar);
            SourceFileSymbol externalSourceFile = subroutine.DeclaringSourceFile;
            NsxModuleBuilder externalNsxBuilder = _compilation.GetNsxModuleBuilder(externalSourceFile);
            _code.WriteUInt16LE(_module.GetExternalModuleToken(externalSourceFile));
            _code.WriteUInt16LE(externalNsxBuilder.GetSubroutineToken(subroutine));
            _code.WriteByte(0);
        }

        private void EmitStatement(StatementSyntax statement)
        {
            switch (statement.Kind)
            {
                case SyntaxNodeKind.Block:
                    EmitBlock((BlockSyntax)statement);
                    break;
                case SyntaxNodeKind.ExpressionStatement:
                    EmitExpression(((ExpressionStatementSyntax)statement).Expression);
                    break;
                case SyntaxNodeKind.IfStatement:
                    EmitIfStatement((IfStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.BreakStatement:
                    EmitBreakStatement((BreakStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.WhileStatement:
                    EmitWhileStatement((WhileStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.ReturnStatement:
                    EmitOpcode(Opcode.Return);
                    break;
                case SyntaxNodeKind.CallChapterStatement:
                    EmitCallChapter((CallChapterStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.CallSceneStatement:
                    EmitCallScene((CallSceneStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectStatement:
                    EmitSelect((SelectStatementSyntax)statement);
                    break;
                case SyntaxNodeKind.SelectSection:
                    EmitSelectSection((SelectSectionSyntax)statement);
                    break;
                case SyntaxNodeKind.DialogueBlock:
                    EmitActivateText();
                    break;
                case SyntaxNodeKind.PXmlString:
                    EmitPresentText((PXmlString)statement);
                    break;
                case SyntaxNodeKind.PXmlLineSeparator:
                    EmitOpcode(Opcode.AwaitInput);
                    break;
            }
        }

        private void EmitBlock(BlockSyntax block)
        {
            foreach (StatementSyntax statement in block.Statements)
            {
                EmitStatement(statement);
            }
        }

        private void EmitIfStatement(IfStatementSyntax ifStmt)
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

        private void EmitWhileStatement(WhileStatementSyntax whileStmt)
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
            BreakScope bodyScope = EmitLoopBody(LoopKind.While, whileStmt.Body);
            EmitJump(Opcode.Jump, loopStart);
            PatchJump(exitJump, _code.Position);
            PatchBreaks(bodyScope, _code.Position);
        }

        private void EmitSelect(SelectStatementSyntax selectStmt)
        {
            int loopStart = _code.Position;
            EmitOpcode(Opcode.SelectStart);
            BreakScope bodyScope = EmitLoopBody(LoopKind.Select, selectStmt.Body);
            EmitOpcode(Opcode.SelectEnd);
            EmitJump(Opcode.Jump, loopStart);
            PatchBreaks(bodyScope, _code.Position);
            EmitOpcode(Opcode.SelectEnd);
        }

        private void EmitSelectSection(SelectSectionSyntax section)
        {
            if (_breakScopes.Count == 0 || _breakScopes.Peek().LoopKind != LoopKind.Select)
            {
                _checker.Report(section, DiagnosticId.OrphanedSelectSection);
                return;
            }

            EmitOpcode(Opcode.IsPressed);
            _code.WriteUInt16LE(_module.GetStringToken(section.Label.Value));
            JumpPlaceholder jmp = EmitJump(Opcode.JumpIfFalse);
            EmitStatement(section.Body);
            EmitBreakPlaceholder();
            PatchJump(jmp, _code.Position);
        }

        private BreakScope EmitLoopBody(LoopKind loopKind, StatementSyntax body)
        {
            _breakScopes.Push(new BreakScope(loopKind));
            EmitStatement(body);
            return _breakScopes.Pop();
        }

        private void EmitBreakStatement(BreakStatementSyntax breakStmt)
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

        private void PatchBreaks(in BreakScope scope, int destination)
        {
            if (scope.NoBreakPlaceholders) { return; }
            Queue<JumpPlaceholder> placeholders = scope.BreakPlaceholders;
            while (placeholders.Count > 0)
            {
                JumpPlaceholder jump = placeholders.Dequeue();
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

        private void EmitActivateText()
        {
            EmitOpcode(Opcode.ActivateBlock);
            _code.WriteUInt16LE((ushort)_textId++);
        }

        private void EmitPresentText(PXmlString text)
        {
            EmitOpcode(Opcode.DisplayLine);
            _code.WriteUInt16LE(_module.GetStringToken(text.Text));
        }

        private void EmitLoadVariable(VariableKind variableKind, ushort token)
        {
            switch (variableKind)
            {
                case VariableKind.Global:
                    EmitOpcode(Opcode.LoadVar);
                    _code.WriteUInt16LE(token);
                    break;
                case VariableKind.Parameter:
                    EmitLoadArg(token);
                    break;
            }
        }

        private void EmitStoreOp(VariableKind variableKind, ushort tk)
        {
            if (variableKind == VariableKind.Global)
            {
                EmitOpcode(Opcode.StoreVar);
                _code.WriteUInt16LE(tk);
            }
            else
            {
                Debug.Assert(variableKind == VariableKind.Parameter);
                EmitStoreArg(tk);
            }
        }

        private void EmitLoadImm(in ConstantValue value)
        {
            switch (value.Type)
            {
                case BuiltInType.Integer:
                    int num = value.AsInteger()!.Value;
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
                            _code.WriteInt32LE(num);
                            break;
                    }
                    break;
                case BuiltInType.DeltaInteger:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteInt32LE(value.AsDelta()!.Value);
                    break;
                case BuiltInType.Boolean:
                    Opcode opcode = value.AsBool()!.Value
                        ? Opcode.LoadImmTrue
                        : Opcode.LoadImmFalse;
                    EmitOpcode(opcode);
                    break;
                case BuiltInType.Float:
                    EmitOpcode(Opcode.LoadImm);
                    _code.WriteByte((byte)value.Type);
                    _code.WriteSingle(value.AsFloat()!.Value);
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
                    _code.WriteByte((byte)value.AsInteger()!.Value);
                    break;
                case BuiltInType.Null:
                    EmitOpcode(Opcode.LoadImmNull);
                    break;
            }
        }

        private void EmitLoadArg(ushort slot)
        {
            Opcode opcode = slot switch
            {
                0 => Opcode.LoadArg0,
                1 => Opcode.LoadArg1,
                2 => Opcode.LoadArg2,
                3 => Opcode.LoadArg3,
                _ => Opcode.LoadArg
            };

            EmitOpcode(opcode);
            if (slot > 3)
            {
                _code.WriteUInt16LE(slot);
            }
        }

        private void EmitStoreArg(ushort slot)
        {
            Opcode opcode = slot switch
            {
                0 => Opcode.StoreArg0,
                1 => Opcode.StoreArg1,
                2 => Opcode.StoreArg2,
                3 => Opcode.StoreArg3,
                _ => Opcode.StoreArg
            };

            EmitOpcode(opcode);
            if (slot > 3)
            {
                _code.WriteUInt16LE(slot);
            }
        }

        private void EmitDialogueBlock(DialogueBlockSyntax dialogueBlock)
        {
            foreach (StatementSyntax statement in dialogueBlock.Parts)
            {
                EmitStatement(statement);
            }
            EmitOpcode(Opcode.Return);
        }
    }
}
