using NitroSharp.NsScriptNew.Syntax;
using NitroSharp.Utilities;

namespace NitroSharp.NsScriptNew
{
    public struct SyntaxTokenEnumerable
    {
        private readonly Lexer _lexer;

        internal SyntaxTokenEnumerable(Lexer lexer)
        {
            _lexer = lexer;
        }

        public Enumerator GetEnumerator() => new Enumerator(_lexer);

        public SyntaxToken[] ToArray()
        {
            var builder = new ArrayBuilder<SyntaxToken>(32);
            foreach (SyntaxToken tk in this)
            {
                builder.Add() = tk;
            }

            return builder.ToArray();
        }

        public ref struct Enumerator
        {
            private readonly Lexer _lexer;
            private SyntaxToken _current;
            private bool _reachedEof;

            internal Enumerator(Lexer lexer)
            {
                _lexer = lexer;
                _current = default;
                _reachedEof = false;
            }

            public SyntaxToken Current => _current;

            public bool MoveNext()
            {
                if (_reachedEof) { return false; }
                _current = _lexer.Lex();
                if (_current.Kind == SyntaxTokenKind.EndOfFileToken)
                {
                    _reachedEof = true;
                }

                return true;
            }
        }
    }
}
