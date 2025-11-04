using System.Collections.Generic;
using System.IO;
using NitroSharp.Common;

namespace NitroSharp.NsScript.Syntax;

public enum SyntaxNodeKind : byte
{
    None,
    SourceFileRoot,
    ErrorStatement,
    ErrorExpression,

    ChapterDeclaration,
    SceneDeclaration,
    FunctionDeclaration,
    Parameter,

    Block,
    IfStatement,
    WhileStatement,
    ExpressionStatement,
    ReturnStatement,
    SelectStatement,
    SelectSection,
    CallSceneStatement,
    CallChapterStatement,
    BreakStatement,

    NameExpression,
    LiteralExpression,
    UnaryExpression,
    BinaryExpression,
    AssignmentExpression,
    FunctionCallExpression,
    BezierExpression,

    DialogueBlock,
    MarkupCodeBlock,
    Markup,
}

public enum SyntaxDumpFormat
{
    Debug,
    RoundtripText
}

public abstract class SyntaxNode(TextSpan span)
{
    private SyntaxTree? _syntaxTree;

    public abstract SyntaxNodeKind Kind { get; }
    public TextSpan Span { get; } = span;

    internal void Bind(SyntaxTree syntaxTree)
    {
        _syntaxTree = syntaxTree;
    }

    public SyntaxTree SyntaxTree => _syntaxTree.NotNull();
    public SourceLocation Location => new(SyntaxTree.SourceText, Span);

    public void Dump(TextWriter textWriter, SyntaxDumpFormat format)
    {
        SyntaxWriter writer = format switch
        {
            SyntaxDumpFormat.Debug => new DebugSyntaxWriter(textWriter),
            SyntaxDumpFormat.RoundtripText => new RoundtripSyntaxWriter(textWriter),
            _ => ThrowHelper.Unreachable<SyntaxWriter>()
        };
        writer.Visit(this);
    }

    public abstract void Accept(SyntaxVisitor visitor);

    protected abstract SyntaxNode? GetChild(int index);

    public Children GetChildren() => new(this);

    public struct Children(SyntaxNode node)
    {
        private int _index = 0;

        public SyntaxNode Current { get; private set; } = null!;

        public Children GetEnumerator() => this;

        public bool MoveNext()
        {
            SyntaxNode? current = node.GetChild(_index);
            if (current is not null)
            {
                Current = current;
                _index++;
                return true;
            }

            Current = null!;
            return false;
        }

        public SyntaxNode[] ToArray()
        {
            if (node.GetChild(0) is null)
            {
                return [];
            }

            var list = new List<SyntaxNode>();
            foreach (SyntaxNode child in this)
            {
                list.Add(child);
            }

            return list.ToArray();
        }
    }
}
