using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using System.Collections.Generic;

namespace NitroSharp.NsScript.IR
{
    public sealed class IRBuilder
    {
        private readonly Implementation _impl = new Implementation();

        public InstructionBlock Build(InvocableSymbol symbol)
        {
            return _impl.Build(symbol);
        }

        private sealed class Implementation : SyntaxVisitor
        {
            private readonly List<Instruction> _instructions = new List<Instruction>();
            private readonly Queue<int> _insertBreaksAt = new Queue<int>();

            public InstructionBlock Build(InvocableSymbol symbol)
            {
                _instructions.Clear();
                var declaration = (MemberDeclaration)symbol.Declaration;
                Visit(declaration.Body);

                _instructions.Add(Instructions.Return());
                return new InstructionBlock(_instructions.ToArray());
            }

            public override void VisitFunction(Function function) => VisitMember(function);
            public override void VisitChapter(Chapter chapter) => VisitMember(chapter);
            public override void VisitScene(Scene scene) => VisitMember(scene);

            private void VisitMember(MemberDeclaration declaration)
            {
                VisitArray(declaration.Body.Statements);
            }
            
            public override void VisitBlock(Block block)
            {
                VisitArray(block.Statements);
            }
            
            public override void VisitIfStatement(IfStatement ifStatement)
            {
                // Two possible scenarios:
                // #1: no else clause
                // <condition>
                // JumpIfEquals (false, <exit>)
                // <consequence>
                // <exit>
                
                // #2: else clause is present
                // <condition>
                // JumpIfEquals (false, <alternative>)
                // <consequence>
                // Jump <exit>
                // <alternative>
                // <exit>
                
                Visit(ifStatement.Condition);

                int firstBranchAt = _instructions.Count; // position where we insert JumpIfEquals (false, <exit> | <alternative>)
                _instructions.Add(Instructions.Nop());

                Visit(ifStatement.IfTrueStatement);
                int firstBranchTarget = _instructions.Count; // <exit>

                if (ifStatement.IfFalseStatement != null)
                {
                    // Scenario #2
                    int secondBranchAt = _instructions.Count; // position where we insert Jump <exit>
                    _instructions.Add(Instructions.Nop());

                    firstBranchTarget = _instructions.Count; // <alternative>
                    Visit(ifStatement.IfFalseStatement);
                    
                    _instructions[secondBranchAt] = Instructions.Jump(_instructions.Count); // <exit>
                }

                _instructions[firstBranchAt] = Instructions.JumpIfEquals(ConstantValue.False, firstBranchTarget);
            }

            public override void VisitWhileStatement(WhileStatement whileStatement)
            {
                // <condition>
                // JumpIfEquals (false, <exit>)
                // <loop_body>
                // Jump <condition>
                // <exit>
                
                int conditionStart = _instructions.Count;
                Visit(whileStatement.Condition);

                int insertBranchAt = _instructions.Count;
                _instructions.Add(Instructions.Nop());

                Visit(whileStatement.Body);

                _instructions.Add(Instructions.Jump(conditionStart));
                int exit = _instructions.Count;
                _instructions[insertBranchAt] = Instructions.JumpIfEquals(ConstantValue.False, exit);

                while (_insertBreaksAt.Count > 0)
                {
                    int pos = _insertBreaksAt.Dequeue();
                    _instructions[pos] = Instructions.Jump(exit);
                } 
            }
            
            public override void VisitBreakStatement(BreakStatement breakStatement)
            {
                _instructions.Add(Instructions.Nop());
                _insertBreaksAt.Enqueue(_instructions.Count - 1);
            }

            public override void VisitReturnStatement(ReturnStatement returnStatement)
            {
                _instructions.Add((Instructions.Return()));
            }

            public override void VisitFunctionCall(FunctionCall functionCall)
            {
                if (functionCall.Target.Symbol != null)
                {
                    var args = functionCall.Arguments;
                    for (int i = args.Length - 1; i >= 0; i--)
                    {
                        Visit(args[i]);
                    }
                }

                _instructions.Add(Instructions.Call(functionCall.Target.Symbol));
            }

            public override void VisitCallChapterStatement(CallChapterStatement syntax)
            {
                _instructions.Add(Instructions.CallFar(syntax.Target.FilePath, "main"));
            }

            public override void VisitCallSceneStatement(CallSceneStatement syntax)
            {
                _instructions.Add(Instructions.CallFar(syntax.TargetFile.FilePath, syntax.SceneName));
            }

            public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
            {
                Visit(expressionStatement.Expression);
            }

            public override void VisitAssignmentExpression(AssignmentExpression expression)
            {
                Visit(expression.Value);
                var target = expression.Target;
                switch (target.Symbol.Kind)
                {
                    case SymbolKind.GlobalVariable:
                        _instructions.Add(Instructions.AssignGlobal(target.Name, expression.OperatorKind));
                        break;
                        
                    case SymbolKind.Parameter:
                        _instructions.Add(Instructions.AssignParameter(target.Name, expression.OperatorKind));
                        break;
                }
            }

            public override void VisitBinaryExpression(BinaryExpression binaryExpression)
            {
                Visit(binaryExpression.Right);
                Visit(binaryExpression.Left);

                _instructions.Add(Instructions.ApplyBinary(binaryExpression.OperatorKind));
            }

            public override void VisitIdentifier(Identifier identifier)
            {
                var symbol = identifier.Symbol;
                if (symbol != null)
                {
                    switch (symbol.Kind)
                    {
                        case SymbolKind.GlobalVariable:
                            _instructions.Add(Instructions.PushGlobal(identifier.Name));
                            break;

                        case SymbolKind.Parameter:
                            _instructions.Add(Instructions.PushLocal(identifier.Name));
                            break;

                        case SymbolKind.EnumValue:
                            var enumValue = (BuiltInEnumValueSymbol)symbol;
                            _instructions.Add(Instructions.PushValue(enumValue.Value));
                            break;
                    }
                }
                else if (!identifier.HasSigil) // likely an unknown enum value
                {
                    _instructions.Add(Instructions.PushValue(ConstantValue.Create(identifier.Name)));
                }
            }

            public override void VisitUnaryExpression(UnaryExpression unaryExpression)
            {
                Visit(unaryExpression.Operand);
                _instructions.Add(Instructions.ApplyUnary(unaryExpression.OperatorKind));
            }

            public override void VisitLiteral(Literal literal)
            {
                _instructions.Add(Instructions.PushValue(literal.Value));
            }

            public override void VisitDeltaExpression(DeltaExpression deltaExpression)
            {
                Visit(deltaExpression.Expression);
                _instructions.Add(Instructions.ConvertToDelta());
            }

            public override void VisitDialogueBlock(DialogueBlock dialogueBlock)
            {
                var symbol = (DialogueBlockSymbol)dialogueBlock.Symbol;
                _instructions.Add(Instructions.SetDialogueBlock(symbol));
            }

            public override void VisitPXmlString(PXmlString pxmlString)
            {
                _instructions.Add(Instructions.Say(pxmlString.Text, pxmlString));
            }

            public override void VisitPXmlLineSeparator(PXmlLineSeparator pxmlLineSeparator)
            {
                _instructions.Add(Instructions.WaitForInput(pxmlLineSeparator));
            }
        }
    }
}
