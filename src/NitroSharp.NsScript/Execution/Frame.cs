using NitroSharp.NsScript.Symbols;
using NitroSharp.NsScript.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Execution
{
    internal sealed class Frame
    {
        private readonly Stack<Continuation> _continuations;

        public Frame(InvocableSymbol symbol)
        {
            Symbol = symbol;
            Arguments = MemorySpace.Empty;

            _continuations = new Stack<Continuation>();
            ContinueWith(symbol.Declaration, false);
        }

        public InvocableSymbol Symbol { get; }
        public MemorySpace Arguments { get; private set; }
        public Statement CurrentStatement => LastContinuation.CurrentStatement;
        public Continuation LastContinuation => _continuations.Peek();
        public bool IsEmpty => _continuations.Count == 0;

        public void SetArgument(string name, ConstantValue value)
        {
            if (ReferenceEquals(Arguments, MemorySpace.Empty))
            {
                Arguments = new MemorySpace();
            }

            Arguments.Set(name, value);
        }

        public bool Advance()
        {
            if (IsEmpty)
            {
                return false;
            }
            if (LastContinuation.Advance())
            {
                return true;
            }

            Continuation c;
            do
            {
                c = _continuations.Pop();
            } while (c.IsAtEnd && !IsEmpty);

            return !IsEmpty;
        }

        public void Break()
        {
            _continuations.Pop();
        }

        public void ContinueWith(ImmutableArray<Statement> statements, bool advance)
        {
            if (advance)
            {
                Advance();
            }

            ContinueWith(statements);
        }

        public void ContinueWith(ImmutableArray<Statement> statements)
        {
            _continuations.Push(new Continuation(statements));
        }

        public void ContinueWith(SyntaxNode node, bool advance)
        {
            if (advance)
            {
                Advance();
            }

            switch (node.Kind)
            {
                case SyntaxNodeKind.Function:
                case SyntaxNodeKind.Scene:
                case SyntaxNodeKind.Chapter:
                    var member = (MemberDeclaration)node;
                    ContinueWith(member.Body.Statements);
                    break;

                case SyntaxNodeKind.Block:
                    var block = (Block)node;
                    if (block.Statements.Length > 0)
                    {
                        ContinueWith(block.Statements);
                    }
                    break;

                case SyntaxNodeKind.Paragraph:
                    var paragraph = (Paragraph)node;
                    ContinueWith(paragraph.Statements);
                    break;

                default:
                    var statement = node as Statement;
                    if (statement == null) throw new InvalidOperationException();
                    ContinueWith(ImmutableArray.Create(statement));
                    break;
            }
        }
    }
}
