using NitroSharp.NsScript.Symbols;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Syntax
{
    public abstract class Declaration : SyntaxNode
    {
        protected Declaration(Identifier name)
        {
            Name = name;
        }

        public Identifier Name { get; }
    }

    public abstract class MemberDeclaration : Declaration
    {
        protected MemberDeclaration(Identifier name, Block body) : base(name)
        {
            Body = body;
        }

        public Block Body { get; }
    }

    public sealed class Chapter : MemberDeclaration
    {
        internal Chapter(Identifier name, Block body) : base(name, body) { }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Chapter;

        public ChapterSymbol ChapterSymbol => (ChapterSymbol)Symbol;

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
        internal Scene(Identifier name, Block body) : base(name, body) { }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Scene;

        public SceneSymbol SceneSymbol => (SceneSymbol)Symbol;

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
        internal Function(Identifier name, ImmutableArray<Parameter> parameters, Block body)
            : base(name, body)
        {
            Parameters = parameters;
        }

        public ImmutableArray<Parameter> Parameters { get; }
        public override SyntaxNodeKind Kind => SyntaxNodeKind.Function;

        public FunctionSymbol FunctionSymbol => (FunctionSymbol)Symbol;

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
        internal Parameter(Identifier name) : base(name) { }

        public override SyntaxNodeKind Kind => SyntaxNodeKind.Parameter;
        public ParameterSymbol ParameterSymbol => (ParameterSymbol)Symbol;

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitParameter(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitParameter(this);
        }
    }
}
