using System;
using System.Collections.Generic;
using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public enum SyntaxNodeKind
    {
        SourceFileRoot,

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
        PXmlString,
        PXmlLineSeparator
    }

    public abstract class SyntaxNode
    {
        protected SyntaxNode(TextSpan span)
        {
            Span = span;
        }

        public abstract SyntaxNodeKind Kind { get; }
        public TextSpan Span { get; }

        public abstract void Accept(SyntaxVisitor visitor);
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        public virtual SyntaxNode? GetNodeSlot(int index)
        {
            return null;
        }

        public Children GetChildren() => new Children(this);

        //public override string ToString()
        //{
        //    var sw = new StringWriter();
        //    var codeWriter = new DefaultCodeWriter(sw);
        //    codeWriter.WriteNode(this);

        //    return sw.ToString();
        //}

        public readonly struct Children
        {
            private readonly SyntaxNode _node;

            public Children(SyntaxNode node)
            {
                _node = node;
            }

            public ChildrenEnumerator GetEnumerator()
                => new ChildrenEnumerator(_node);

            public SyntaxNode?[] ToArray()
            {
                if (_node.GetNodeSlot(0) == null)
                {
                    return Array.Empty<SyntaxNode>();
                }

                var list = new List<SyntaxNode?>();
                foreach (SyntaxNode? child in this)
                {
                    list.Add(child);
                }

                return list.ToArray();
            }
        }

        public struct ChildrenEnumerator
        {
            private readonly SyntaxNode _node;
            private SyntaxNode? _current;
            private int _index;

            public ChildrenEnumerator(SyntaxNode node)
            {
                _node = node;
                _index = 0;
                _current = null;
            }

            public SyntaxNode? Current => _current;

            public bool MoveNext()
            {
                _current = _node.GetNodeSlot(_index);
                if (_current != null)
                {
                    _index++;
                    return true;
                }

                return false;
            }
        }
    }
}
