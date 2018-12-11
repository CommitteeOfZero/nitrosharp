using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Binding;
using NitroSharp.NsScriptNew.Symbols;
using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.NsScriptNew.Text;

namespace NitroSharp.NsScriptNew
{
    public sealed class NsScriptSession
    {
        private readonly Func<FilePath, SourceText> _sourceFileResolver;
        private readonly Dictionary<FilePath, SyntaxTree> _syntaxTrees;
        private readonly Dictionary<SyntaxTree, ModuleSymbol> _moduleSymbols;
        private readonly Dictionary<SourceFileSymbol, SourceFileBinder> _binders;

        public NsScriptSession(Func<FilePath, SourceText> sourceFileResolver)
        {
            _sourceFileResolver = sourceFileResolver
                ?? throw new ArgumentNullException(nameof(sourceFileResolver));

            _syntaxTrees = new Dictionary<FilePath, SyntaxTree>();
            _moduleSymbols = new Dictionary<SyntaxTree, ModuleSymbol>();
            _binders = new Dictionary<SourceFileSymbol, SourceFileBinder>();
        }

        public ModuleSymbol GetModuleSymbol(SyntaxTree syntaxTree)
        {
            if (_moduleSymbols.TryGetValue(syntaxTree, out ModuleSymbol symbol))
            {
                return symbol;
            }

            symbol = CreateModuleSymbol(syntaxTree);
            _moduleSymbols[syntaxTree] = symbol;
            return symbol;
        }

        public BoundBlock BindMember(MemberSymbol memberSymbol)
        {
            SourceFileSymbol sourceFile = memberSymbol.DeclaringSourceFile;
            if (!_binders.TryGetValue(sourceFile, out SourceFileBinder binder))
            {
                binder = new SourceFileBinder(sourceFile);
                _binders[sourceFile] = binder;
            }

            return binder.BindMember(memberSymbol);
        }

        private ModuleSymbol CreateModuleSymbol(SyntaxTree syntaxTree)
        {
            var root = syntaxTree.Root as SourceFileRootSyntax;
            if (root.FileReferences.Length == 0)
            {
                return new ModuleSymbol(ImmutableArray.Create(syntaxTree));
            }

            var builder = ImmutableArray.CreateBuilder<SyntaxTree>(4);
            builder.Add(syntaxTree);
            CollectReferences(syntaxTree, builder);
            return new ModuleSymbol(builder.ToImmutable());
        }

        private void CollectReferences(SyntaxTree syntaxTree, ImmutableArray<SyntaxTree>.Builder builder)
        {
            var root = (SourceFileRootSyntax)syntaxTree.Root;
            foreach (Spanned<string> include in root.FileReferences)
            {
                SyntaxTree tree = GetSyntaxTree(include.Value);
                builder.Add(tree);
                CollectReferences(tree, builder);
            }
        }

        public SyntaxTree GetSyntaxTree(FilePath filePath)
        {
            if (_syntaxTrees.TryGetValue(filePath, out SyntaxTree syntaxTree))
            {
                return syntaxTree;
            }

            SourceText sourceText = _sourceFileResolver.Invoke(filePath);
            syntaxTree = Parsing.ParseText(sourceText);
            _syntaxTrees[filePath] = syntaxTree;
            return syntaxTree;
        }
    }
}
