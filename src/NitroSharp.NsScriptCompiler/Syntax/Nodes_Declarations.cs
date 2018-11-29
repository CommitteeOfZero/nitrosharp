using System.Collections.Immutable;

namespace NitroSharp.NsScriptNew.Syntax
{
    public abstract class Declaration : Statement
    {
        protected Declaration(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public abstract class MemberDeclaration : Declaration
    {
        protected MemberDeclaration(string name, Block body) : base(name)
        {
            Body = body;
        }

        public Block Body { get; }
    }

    public sealed class Chapter : MemberDeclaration
    {
        internal Chapter(string name, Block body) : base(name, body) { }

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

    public sealed class Scene : MemberDeclaration
    {
        internal Scene(string name, Block body) : base(name, body) { }

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

    public sealed class Function : MemberDeclaration
    {
        internal Function(string name, ImmutableArray<Parameter> parameters, Block body)
            : base(name, body)
        {
            Parameters = parameters;
        }

        public ImmutableArray<Parameter> Parameters { get; }
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

    public sealed class Parameter : Declaration
    {
        internal Parameter(string name) : base(name) { }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitParameter(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitParameter(this);
        }
    }

    /// <summary>
    /// Also known as a &lt;PRE&gt; element.
    /// </summary>
    public sealed class DialogueBlock : MemberDeclaration
    {
        internal DialogueBlock(string name, string associatedBox, Block body)
            : base(name, body)
        {
            AssociatedBox = associatedBox;
        }

        public string AssociatedBox { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.DialogueBlock;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitDialogueBlock(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitDialogueBlock(this);
        }
    }
}
