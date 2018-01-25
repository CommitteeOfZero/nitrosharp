using NitroSharp.NsScript.Syntax;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class SymbolTableBuilder : SyntaxVisitor<NamedSymbol>
    {
        private readonly Stack<SymbolTable> _scopes = new Stack<SymbolTable>();
        private SymbolTable CurrentScope => _scopes.Peek();

        private SymbolTable BeginScope(bool empty = false)
        {
            var scope = !empty ? new SymbolTable() : SymbolTable.Empty;
            _scopes.Push(scope);
            return scope;
        }

        private void EndScope() => _scopes.Pop();

        public override NamedSymbol Visit(SyntaxNode node)
        {
            var symbol = base.Visit(node);
            if (symbol != null)
            {
                if (node.Kind != SyntaxNodeKind.SourceFile)
                {
                    CurrentScope.Declare(symbol);
                }
            }

            node.Symbol = symbol;
            return symbol;
        }

        public override NamedSymbol VisitSourceFile(SourceFile sourceFile)
        {
            var members = BeginScope();
            VisitArray(sourceFile.Members);
            EndScope();

            return new SourceFileSymbol(sourceFile.FileName, sourceFile, members);
        }

        public override NamedSymbol VisitChapter(Chapter chapter)
        {
            return new ChapterSymbol(chapter.Identifier.Name, chapter);
        }

        public override NamedSymbol VisitScene(Scene scene)
        {
            return new SceneSymbol(scene.Identifier.Name, scene);
        }

        public override NamedSymbol VisitFunction(Function function)
        {
            var locals = BeginScope(empty: false);
            VisitArray(function.Parameters);
            Visit(function.Body);
            EndScope();

            return new FunctionSymbol(function.Identifier.Name, function, locals);
        }

        public override NamedSymbol VisitBlock(Block block)
        {
            foreach (var stmt in block.Statements)
            {
                if (stmt is Declaration)
                {
                    Visit(stmt);
                }
                // The body of an if statement can contain dialogue blocks.
                else if (stmt is IfStatement ifStatement)
                {
                    Visit(ifStatement.IfTrueStatement);
                    if (ifStatement.IfFalseStatement != null)
                    {
                        Visit(ifStatement.IfFalseStatement);
                    }
                }
            }

            return null;
        }

        public override NamedSymbol VisitParameter(Parameter parameter)
        {
            return new ParameterSymbol(parameter.Identifier.Name, parameter);
        }

        public override NamedSymbol VisitDialogueBlock(DialogueBlock dialogueBlock)
        {
            return new DialogueBlockSymbol(dialogueBlock.Identifier.Name, dialogueBlock);
        }
    }
}
