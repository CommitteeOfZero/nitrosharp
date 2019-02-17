using NitroSharp.NsScript.Syntax;
using NitroSharp.NsScript.Text;
using System;

namespace NitroSharp.NsScript
{
    public sealed class SyntaxTree
    {
        private readonly DiagnosticBuilder _diagnosticBuilder;
        private DiagnosticBag? _diagnostics;

        internal SyntaxTree(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnosticBuilder)
        {
            SourceText = sourceText;
            Root = root;
            _diagnosticBuilder = diagnosticBuilder;
        }

        public SyntaxNode Root { get; }
        public SourceText SourceText { get; }
        public DiagnosticBag Diagnostics
        {
            get
            {
                if (_diagnostics == null || _diagnostics.All.Length != _diagnosticBuilder.Count)
                {
                    _diagnostics = _diagnosticBuilder.ToImmutableBag();
                }

                return _diagnostics;
            }
        }
    }
}
