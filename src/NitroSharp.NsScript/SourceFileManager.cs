using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace NitroSharp.NsScript
{
    /// <summary>
    /// Responsible for parsing, analyzing and caching source files and their dependencies.
    /// </summary>
    public class SourceFileManager
    {
        private readonly Func<SourceFileReference, Stream> _locateFunc;
        private readonly Dictionary<SourceFileReference, SyntaxTree> _syntaxTrees;
        private readonly Dictionary<SourceFileReference, MergedSourceFileSymbol> _mergedSourceFileSymbols;

        private readonly SymbolTableBuilder _symbolTableBuilder;
        private readonly Binder _binder;

        public SourceFileManager(Func<SourceFileReference, Stream> sourceFileLocator)
        {
            _locateFunc = sourceFileLocator;
            _syntaxTrees = new Dictionary<SourceFileReference, SyntaxTree>();
            _mergedSourceFileSymbols = new Dictionary<SourceFileReference, MergedSourceFileSymbol>();

            _symbolTableBuilder = new SymbolTableBuilder();
            _binder = new Binder();
        }

        public MergedSourceFileSymbol Resolve(SourceFileReference sourceFileReference)
        {
            if (_mergedSourceFileSymbols.TryGetValue(sourceFileReference, out var symbol))
            {
                return symbol;
            }

            symbol = Load(sourceFileReference);
            _mergedSourceFileSymbols[sourceFileReference] = symbol;
            return symbol;
        }

        private MergedSourceFileSymbol Load(SourceFileReference sourceFileReference)
        {
            var recursiveReferences = new HashSet<SourceFileReference>();
            var syntaxTree = Parse(sourceFileReference, recursiveReferences);
            var sourceFile = (SourceFile)syntaxTree.Root;

            var mergedSymbol = CreateMergedSourceFileSymbol(sourceFile, recursiveReferences);
            _binder.Bind(sourceFile, mergedSymbol);

            foreach (var dependency in mergedSymbol.Dependencies)
            {
                sourceFile = dependency.Declaration;
                if (!sourceFile.IsBound)
                {
                    _binder.Bind(sourceFile, mergedSymbol);
                }
            }

            return mergedSymbol;
        }

        private SyntaxTree Parse(SourceFileReference sourceFileReference, ISet<SourceFileReference> recursiveReferences)
        {
            var text = SourceText.From(_locateFunc(sourceFileReference), sourceFileReference.FileName);
            var syntaxTree = Parsing.ParseText(text);
            _syntaxTrees[sourceFileReference] = syntaxTree;

            var root = (SourceFile)syntaxTree.Root;
            foreach (var reference in root.FileReferences)
            {
                recursiveReferences.Add(reference);
                if (!_syntaxTrees.ContainsKey(reference))
                {
                    try
                    {
                        Parse(reference, recursiveReferences);
                    }
                    catch
                    {
                        recursiveReferences.Remove(reference);
                    }
                }
            }

            _symbolTableBuilder.Build(syntaxTree);
            return syntaxTree;
        }

        private MergedSourceFileSymbol CreateMergedSourceFileSymbol(SourceFile sourceFile, ISet<SourceFileReference> references)
        {
            var dependencies = references.Select(x => _syntaxTrees[x].Root.Symbol as SourceFileSymbol).ToImmutableArray();
            return new MergedSourceFileSymbol(sourceFile.SourceFileSymbol, dependencies);
        }
    }
}
