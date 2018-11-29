using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.NsScriptNew.Text;
using System;

namespace NitroSharp.NsScriptNew
{
    public sealed class SyntaxTree
    {
        private readonly DiagnosticBuilder _diagnosticBuilder;
        private DiagnosticBag _diagnostics;

        internal static SyntaxTree Create(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnosticBuilder)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            if (diagnosticBuilder == null)
            {
                throw new ArgumentNullException(nameof(diagnosticBuilder));
            }

            return new SyntaxTree(sourceText, root, diagnosticBuilder);
        }

        private SyntaxTree(SourceText sourceText, SyntaxNode root, DiagnosticBuilder diagnosticBuilder)
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
