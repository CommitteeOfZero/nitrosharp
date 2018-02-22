using NitroSharp.NsScript.Symbols;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public sealed class SourceFile : SyntaxNode
    {
        internal SourceFile(ImmutableArray<MemberDeclaration> members, ImmutableArray<SourceFileReference> fileReferences)
        {
            Members = members;
            FileReferences = fileReferences;
        }

        public ImmutableArray<MemberDeclaration> Members { get; }

        /// <summary>
        /// Direct file references (aka 'includes').
        /// </summary>
        public ImmutableArray<SourceFileReference> FileReferences { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.SourceFile;
        public SourceFileSymbol SourceFileSymbol => (SourceFileSymbol)Symbol;
        public bool IsBound => Symbol != null;

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
