using System;
using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Text;

namespace NitroSharp.NsScriptNew.Syntax
{
    public abstract class StatementSyntax : SyntaxNode
    {
        protected StatementSyntax(TextSpan span) : base(span)
        {
        }
    }

    public sealed class BlockSyntax : StatementSyntax
    {
        internal BlockSyntax(ImmutableArray<StatementSyntax> statements, TextSpan span)
            : base(span)
        {
            Statements = statements;
        }

        public ImmutableArray<StatementSyntax> Statements { get; }
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

    public class ExpressionStatementSyntax : StatementSyntax
    {
        internal ExpressionStatementSyntax(ExpressionSyntax expression, TextSpan span)
            : base(span)
        {
            Expression = expression;
        }

        public ExpressionSyntax Expression { get; }
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

    public sealed class IfStatementSyntax : StatementSyntax
    {
        internal IfStatementSyntax(
            ExpressionSyntax condition,
            StatementSyntax ifTrueStatement,
            StatementSyntax ifFalseStatement,
            TextSpan span) : base(span)
        {
            Condition = condition;
            IfTrueStatement = ifTrueStatement;
            IfFalseStatement = ifFalseStatement;
        }

        public ExpressionSyntax Condition { get; }
        public StatementSyntax IfTrueStatement { get; }
        public StatementSyntax IfFalseStatement { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.IfStatement;

        public override SyntaxNode GetNodeSlot(int index)
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

    public sealed class BreakStatementSyntax : StatementSyntax
    {
        internal BreakStatementSyntax(TextSpan span) : base(span)
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

    public sealed class WhileStatementSyntax : StatementSyntax
    {
        internal WhileStatementSyntax(
            ExpressionSyntax condition,
            StatementSyntax body,
            TextSpan span) : base(span)
        {
            Condition = condition;
            Body = body;
        }

        public ExpressionSyntax Condition { get; }
        public StatementSyntax Body { get; }

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

    public sealed class ReturnStatementSyntax : StatementSyntax
    {
        internal ReturnStatementSyntax(TextSpan span) : base(span)
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

    public sealed class SelectStatementSyntax : StatementSyntax
    {
        internal SelectStatementSyntax(BlockSyntax body, TextSpan span)
            : base(span)
        {
            Body = body;
        }

        public BlockSyntax Body { get; }
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

    public sealed class SelectSectionSyntax : StatementSyntax
    {
        internal SelectSectionSyntax(Spanned<string> label, BlockSyntax body, TextSpan span)
            : base(span)
        {
            Label = label;
            Body = body;
        }

        public Spanned<string> Label { get; }
        public BlockSyntax Body { get; }

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

    public sealed class CallChapterStatementSyntax : StatementSyntax
    {
        internal CallChapterStatementSyntax(Spanned<string> targetModule, TextSpan span)
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

    public sealed class CallSceneStatementSyntax : StatementSyntax
    {
        internal CallSceneStatementSyntax(
            Spanned<string>? targetFile,
            Spanned<string> targetScene,
            TextSpan span) : base(span)
        {
            TargetFile = targetFile;
            TargetScene = targetScene;
        }

        public Spanned<string>? TargetFile { get; }
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

    public sealed class PXmlString : StatementSyntax
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

    public sealed class PXmlLineSeparator : StatementSyntax
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
