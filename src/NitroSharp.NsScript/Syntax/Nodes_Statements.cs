using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class Statement : SyntaxNode
    {
    }

    public sealed class Block : Statement
    {
        internal Block(ImmutableArray<Statement> statements)
        {
            Statements = statements;
        }

        public ImmutableArray<Statement> Statements { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Block;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitBlock(this);
        }
    }

    public class ExpressionStatement : Statement
    {
        internal ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }

        public Expression Expression { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.ExpressionStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }

    public sealed class IfStatement : Statement
    {
        internal IfStatement(Expression condition, Statement ifTrueStatement, Statement ifFalseStatement)
        {
            Condition = condition;
            IfTrueStatement = ifTrueStatement;
            IfFalseStatement = ifFalseStatement;
        }

        public Expression Condition { get; }
        public Statement IfTrueStatement { get; }
        public Statement IfFalseStatement { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitIfStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitIfStatement(this);
        }
    }

    public sealed class BreakStatement : Statement
    {
        public override SyntaxNodeKind Kind => SyntaxNodeKind.BreakStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitBreakStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitBreakStatement(this);
        }
    }

    public sealed class WhileStatement : Statement
    {
        internal WhileStatement(Expression condition, Statement body)
        {
            Condition = condition;
            Body = body;
        }

        public Expression Condition { get; }
        public Statement Body { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.WhileStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitWhileStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitWhileStatement(this);
        }
    }

    public sealed class ReturnStatement : Statement
    {
        public override SyntaxNodeKind Kind => SyntaxNodeKind.ReturnStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitReturnStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitReturnStatement(this);
        }
    }

    public sealed class SelectStatement : Statement
    {
        internal SelectStatement(Block body)
        {
            Body = body;
        }

        public Block Body { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.SelectStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitSelectStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitSelectStatement(this);
        }
    }

    public sealed class SelectSection : Statement
    {
        internal SelectSection(Identifier label, Block body)
        {
            Label = label;
            Body = body;
        }

        public Identifier Label { get; }
        public Block Body { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.SelectSection;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitSelectSection(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitSelectSection(this);
        }
    }

    public sealed class CallChapterStatement : Statement
    {
        internal CallChapterStatement(SourceFileReference target)
        {
            Target = target;
        }

        public SourceFileReference Target { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.CallChapterStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitCallChapterStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitCallChapterStatement(this);
        }
    }

    public sealed class CallSceneStatement : Statement
    {
        internal CallSceneStatement(SourceFileReference targetFile, string sceneName)
        {
            TargetFile = targetFile;
            SceneName = sceneName;
        }

        public SourceFileReference TargetFile { get; }
        public string SceneName { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.CallSceneStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitCallSceneStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitCallSceneStatement(this);
        }
    }

    public sealed class PXmlString : Statement
    {
        internal PXmlString(string text)
        {
            Text = text;
        }

        public string Text { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.PXmlString;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitPXmlString(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitPXmlString(this);
        }
    }

    public sealed class PXmlLineSeparator : Statement
    {
        public override SyntaxNodeKind Kind => SyntaxNodeKind.PXmlLineSeparator;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitPXmlLineSeparator(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitPXmlLineSeparator(this);
        }
    }
}
