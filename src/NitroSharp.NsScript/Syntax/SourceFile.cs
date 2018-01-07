using NitroSharp.NsScript.Symbols;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public sealed class SourceFile : SyntaxNode
    {
        internal SourceFile(string fileName, ImmutableArray<MemberDeclaration> members, ImmutableArray<SourceFileReference> fileReferences)
        {
            FileName = fileName;
            Members = members;
            FileReferences = fileReferences;
        }

        public string FileName { get; }
        public ImmutableArray<MemberDeclaration> Members { get; }

        /// <summary>
        /// Direct file references (aka 'includes').
        /// </summary>
        public ImmutableArray<SourceFileReference> FileReferences { get; }

        public bool IsBound { get; internal set; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.SourceFile;
        public SourceFileSymbol SourceFileSymbol => (SourceFileSymbol)Symbol;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitSourceFile(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitSourceFile(this);
        }
    }
}
