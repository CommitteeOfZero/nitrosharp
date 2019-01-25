using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Text;

namespace NitroSharp.NsScriptNew.Syntax
{
    public sealed class SourceFileRootSyntax : SyntaxNode
    {
        internal SourceFileRootSyntax(
            ImmutableArray<MemberDeclarationSyntax> memberDeclarations,
            ImmutableArray<Spanned<string>> fileReferences,
            (uint chapterCount, uint sceneCount, uint functionCount) memberCounts,
            TextSpan span) : base(span)
        {
            MemberDeclarations = memberDeclarations;
            FileReferences = fileReferences;
            (ChapterCount, SceneCount, FunctionCount) = memberCounts;
        }

        public ImmutableArray<MemberDeclarationSyntax> MemberDeclarations { get; }
        public ImmutableArray<Spanned<string>> FileReferences { get; }

        public uint ChapterCount { get; }
        public uint SceneCount { get; }
        public uint FunctionCount { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.SourceFileRoot;

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
