using System;
using System.Collections.Immutable;

namespace CommitteeOfZero.NsScript
{
    public abstract class Statement : SyntaxNode
    {
    }

    public interface IJumpTarget
    {
        Identifier Name { get; }
        Block Body { get; }
    }

    public interface IBlock
    {
        ImmutableArray<Statement> Statements { get; }
    }

    public static class StatementFactory
    {
        public static Chapter Chapter(Identifier name, Block body) => new Chapter(name, body);
        public static Scene Scene(Identifier name, Block body) => new Scene(name, body);

        public static Function Function(Identifier name, ImmutableArray<ParameterReference> parameters, Block body) =>
            new Function(name, parameters, body);

        public static Block Block(ImmutableArray<Statement> statements) => new Block(statements);

        public static ExpressionStatement ExpressionStatement(Expression expression) =>
            new ExpressionStatement(expression);

        public static IfStatement If(Expression condition, Statement ifTrueStatement, Statement ifFalseStatement) =>
            new IfStatement(condition, ifTrueStatement, ifFalseStatement);

        public static BreakStatement Break() => new BreakStatement();

        public static WhileStatement While(Expression condition, Statement body) =>
            new WhileStatement(condition, body);

        public static ReturnStatement Return() => new ReturnStatement();

        public static SelectStatement Select(Block body) => new SelectStatement(body);
        public static SelectSection SelectSection(Identifier label, Block body) => new SelectSection(label, body);

        public static CallChapterStatement CallChapter(Identifier chapterName) => new CallChapterStatement(chapterName);
        public static CallSceneStatement CallScene(Identifier sceneName) => new CallSceneStatement(sceneName);

        public static Paragraph Paragraph(string blockIdentifier, string boxName, ImmutableArray<Statement> statements) =>
            new Paragraph(blockIdentifier, boxName, statements);

        public static PXmlString PXmlString(string text) => new PXmlString(text);
    }

    public sealed class Paragraph : Statement, IBlock
    {
        internal Paragraph(string identifier, string associatedBox, ImmutableArray<Statement> statements)
        {
            Identifier = identifier;
            AssociatedBox = associatedBox;
            Statements = statements;
        }

        public string Identifier { get; }
        public string AssociatedBox { get; }
        public ImmutableArray<Statement> Statements { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Paragraph;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitParagraph(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitParagraph(this);
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

    public sealed class Chapter : Statement, IJumpTarget
    {
        internal Chapter(Identifier name, Block body)
        {
            Name = name;
            Body = body;
        }

        public Identifier Name { get; }
        public Block Body { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Chapter;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitChapter(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitChapter(this);
        }
    }

    public sealed class Scene : Statement, IJumpTarget
    {
        internal Scene(Identifier name, Block body)
        {
            Name = name;
            Body = body;
        }

        public Identifier Name { get; }
        public Block Body { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Scene;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitScene(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitScene(this);
        }
    }

    public sealed class Function : Statement, IJumpTarget
    {
        internal Function(Identifier name, ImmutableArray<ParameterReference> parameters, Block body)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }

        public Identifier Name { get; }
        public ImmutableArray<ParameterReference> Parameters { get; }
        public Block Body { get; }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Function;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitFunction(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitFunction(this);
        }
    }

    public sealed class Block : Statement, IBlock
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
        internal CallChapterStatement(Identifier chapterName)
        {
            ChapterName = chapterName;
        }

        public Identifier ChapterName { get; }
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
        internal CallSceneStatement(Identifier sceneName)
        {
            SceneName = sceneName;
        }

        public Identifier SceneName { get; }
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
}
