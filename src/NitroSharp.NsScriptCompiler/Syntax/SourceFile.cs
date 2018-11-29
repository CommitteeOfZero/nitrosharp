using System.Collections.Immutable;

namespace NitroSharp.NsScriptNew.Syntax
{
    public sealed class SourceFile : SyntaxNode
    {
        internal SourceFile(
            ImmutableArray<MemberDeclaration> members,
            ImmutableArray<SourceFileReference> fileReferences)
        {
            Members = members;
            FileReferences = fileReferences;
        }

        public ImmutableArray<MemberDeclaration> Members { get; }
        public ImmutableArray<SourceFileReference> FileReferences { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.SourceFile;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new System.NotImplementedException();
        }
    }
}
