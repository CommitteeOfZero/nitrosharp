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
            if (node.Kind != SyntaxNodeKind.SourceFile)
            {
                CurrentScope.Declare(symbol);
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
            return new ChapterSymbol(chapter.Name.Value, chapter);
        }

        public override NamedSymbol VisitScene(Scene scene)
        {
            return new SceneSymbol(scene.Name.Value, scene);
        }

        public override NamedSymbol VisitFunction(Function function)
        {
            var parameters = BeginScope(empty: function.Parameters.IsEmpty);
            VisitArray(function.Parameters);
            EndScope();

            return new FunctionSymbol(function.Name.Value, function, parameters);
        }

        public override NamedSymbol VisitParameter(Parameter parameter)
        {
            return new ParameterSymbol(parameter.Name.Value, parameter);
        }
    }
}
