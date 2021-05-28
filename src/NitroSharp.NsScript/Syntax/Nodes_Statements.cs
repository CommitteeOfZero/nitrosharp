using System;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class Statement : SyntaxNode
    {
        protected Statement(TextSpan span) : base(span)
        {
        }
    }

    public sealed class Block : Statement
    {
        internal Block(ImmutableArray<Statement> statements, TextSpan span) : base(span)
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
        internal ExpressionStatement(Expression expression, TextSpan span) : base(span)
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
        internal IfStatement(
            Expression condition,
            Statement ifTrueStatement,
            Statement? ifFalseStatement,
            TextSpan span) : base(span)
        {
            Condition = condition;
            IfTrueStatement = ifTrueStatement;
            IfFalseStatement = ifFalseStatement;
        }

        public Expression Condition { get; }
        public Statement IfTrueStatement { get; }
        public Statement? IfFalseStatement { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;

        public override SyntaxNode? GetNodeSlot(int index)
        {
            switch (index)
            {
                case 0: return Condition;
                case 1: return IfTrueStatement;
                case 2: return IfFalseStatement;
                default: return null;
            }
        }

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
        internal BreakStatement(TextSpan span) : base(span)
        {
        }

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
        internal WhileStatement(Expression condition, Statement body, TextSpan span)
            : base(span)
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
        internal ReturnStatement(TextSpan span) : base(span)
        {
        }

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
        internal SelectStatement(Block body, TextSpan span)
            : base(span)
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
        internal SelectSection(Spanned<string> label, Block body, TextSpan span)
            : base(span)
        {
            Label = label;
            Body = body;
        }

        public Spanned<string> Label { get; }
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
        internal CallChapterStatement(Spanned<string> targetModule, TextSpan span)
            : base(span)
        {
            TargetModule = targetModule;
        }

        public Spanned<string> TargetModule { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.CallChapterStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new NotImplementedException();
            //visitor.VisitCallChapterStatement(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class CallSceneStatement : Statement
    {
        internal CallSceneStatement(
            Spanned<string>? targetFile,
            Spanned<string> targetScene,
            TextSpan span) : base(span)
        {
            TargetModule = targetFile;
            TargetScene = targetScene;
        }

        public Spanned<string>? TargetModule { get; }
        public Spanned<string> TargetScene { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.CallSceneStatement;

        public override void Accept(SyntaxVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PXmlString : Statement
    {
        internal PXmlString(string text, TextSpan span) : base(span)
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
        internal PXmlLineSeparator(TextSpan span) : base(span)
        {
        }

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
