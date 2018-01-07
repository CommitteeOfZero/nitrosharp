using NitroSharp.NsScript.Syntax;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Execution
{
    internal sealed class Continuation
    {
        private int _position;
        private readonly ImmutableArray<Statement> _statements;

        public Continuation(ImmutableArray<Statement> statements)
        {
            _statements = statements;
        }

        public Statement CurrentStatement => _statements[_position];
        public bool IsAtEnd => _position >= _statements.Length;

        public bool Advance()
        {
            if (_position < _statements.Length - 1)
            {
                _position = _position + 1;
                return true;
            }

            return false;
        }
    }
}
