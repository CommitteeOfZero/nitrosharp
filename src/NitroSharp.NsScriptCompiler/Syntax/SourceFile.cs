using System.Collections.Immutable;

namespace NitroSharp.NsScriptNew.Syntax
{
    public sealed class SourceFile : SyntaxNode
    {
        internal SourceFile(
            ImmutableArray<MemberDeclarationSyntax> members,
            ImmutableArray<Spanned<string>> fileReferences)
        {
            MemberDeclarations = members;
            FileReferences = fileReferences;
        }

        public ImmutableArray<MemberDeclarationSyntax> MemberDeclarations { get; }
        public ImmutableArray<Spanned<string>> FileReferences { get; }

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
