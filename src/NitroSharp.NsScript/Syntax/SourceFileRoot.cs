using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax;

public sealed class SourceFileRoot : SyntaxNode
{
    internal SourceFileRoot(
        ImmutableArray<SubroutineDeclaration> subroutineDeclarations,
        ImmutableArray<Spanned<string>> fileReferences,
        (uint chapterCount, uint sceneCount, uint functionCount) subroutineCounts,
        TextSpan span) : base(span)
    {
        SubroutineDeclarations = subroutineDeclarations;
        FileReferences = fileReferences;
        (ChapterCount, SceneCount, FunctionCount) = subroutineCounts;
    }

    public ImmutableArray<SubroutineDeclaration> SubroutineDeclarations { get; }
    public ImmutableArray<Spanned<string>> FileReferences { get; }

    public uint ChapterCount { get; }
    public uint SceneCount { get; }
    public uint FunctionCount { get; }

    public override SyntaxNodeKind Kind => SyntaxNodeKind.SourceFileRoot;

    protected override SyntaxNode? GetChild(int index)
    {
        return index < SubroutineDeclarations.Length ? SubroutineDeclarations[index] : null;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitSourceFileRoot(this);
    }
}
