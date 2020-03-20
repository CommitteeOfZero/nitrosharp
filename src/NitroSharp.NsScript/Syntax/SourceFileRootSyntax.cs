using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public sealed class SourceFileRootSyntax : SyntaxNode
    {
        internal SourceFileRootSyntax(
            ImmutableArray<SubroutineDeclarationSyntax> subroutineDeclarations,
            ImmutableArray<Spanned<string>> fileReferences,
            (uint chapterCount, uint sceneCount, uint functionCount) subroutineCounts,
            TextSpan span) : base(span)
        {
            SubroutineDeclarations = subroutineDeclarations;
            FileReferences = fileReferences;
            (ChapterCount, SceneCount, FunctionCount) = subroutineCounts;
        }

        public ImmutableArray<SubroutineDeclarationSyntax> SubroutineDeclarations { get; }
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
