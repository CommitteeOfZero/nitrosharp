using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    /// <summary>
    /// Binds identifiers to symbols.
    /// </summary>
    public sealed class Binder : SyntaxVisitor
    {
        private MergedSourceFileSymbol _mergedSymbol;
        private FunctionSymbol _currentFunction;

        public void Bind(SourceFile sourceFile, MergedSourceFileSymbol context)
        {
            _mergedSymbol = context;
            Visit(sourceFile);
            sourceFile.IsBound = true;
        }

        public override void VisitSourceFile(SourceFile sourceFile)
        {
            VisitArray(sourceFile.Members);
        }

        public override void VisitChapter(Chapter chapter)
        {
            Visit(chapter.Body);
        }

        public override void VisitFunction(Function function)
        {
            _currentFunction = function.FunctionSymbol;
            Visit(function.Body);
            _currentFunction = null;
        }

        public override void VisitDialogueBlock(DialogueBlock paragraph)
        {
            Visit(paragraph.Body);
        }

        public override void VisitBlock(Block block)
        {
            VisitArray(block.Statements);
        }

        public override void VisitWhileStatement(WhileStatement whileStatement)
        {
            Visit(whileStatement.Condition);
            Visit(whileStatement.Body);
        }

        public override void VisitIfStatement(IfStatement ifStatement)
        {
            Visit(ifStatement.Condition);
            Visit(ifStatement.IfTrueStatement);
            Visit(ifStatement.IfFalseStatement);
        }

        public override void VisitSelectStatement(SelectStatement selectStatement)
        {
            Visit(selectStatement.Body);
        }

        public override void VisitSelectSection(SelectSection selectSection)
        {
            Visit(selectSection.Body);
        }

        public override void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            Visit(expressionStatement.Expression);
        }

        public override void VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            Visit(unaryExpression.Operand);
        }

        public override void VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            Visit(binaryExpression.Left);
            Visit(binaryExpression.Right);
        }

        public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            Visit(assignmentExpression.Value);
            Visit(assignmentExpression.Target);
        }

        public override void VisitDeltaExpression(DeltaExpression deltaExpression)
        {
            Visit(deltaExpression.Expression);
        }

        public override void VisitFunctionCall(FunctionCall functionCall)
        {
            string targetName = functionCall.Target.Name;
            var functionSymbol = BuiltInFunctions.Symbols.Lookup(targetName);
            if (functionSymbol != null)
            {
                functionCall.Target.Symbol = functionSymbol;
            }
            else
            {
                functionSymbol = _mergedSymbol.LookupFunction(targetName);
                functionCall.Target.Symbol = functionSymbol;
            }

            VisitArray(functionCall.Arguments);
        }

        public override void VisitIdentifier(Identifier identifier)
        {
            if (_currentFunction != null && _currentFunction.TryLookupParameter(identifier.Name, out var parameter))
            {
                identifier.Symbol = parameter;
                return;
            }

            if (identifier.IsGlobalVariable)
            {
                identifier.Symbol = GlobalVariableSymbol.Instance;
                return;
            }
            else
            {
                identifier.Symbol = EnumValueSymbols.Symbols.Lookup(identifier.Name);
            }
        }
    }
}
