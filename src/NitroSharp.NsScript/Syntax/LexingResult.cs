using System;
using NitroSharp.Utilities;

namespace NitroSharp.NsScript.Syntax;

public readonly struct LexingResult
{
    private readonly Lexer _lexer;

    internal LexingResult(Lexer lexer)
    {
        _lexer = lexer;
    }

    public DiagnosticCollection Diagnostics => _lexer.Diagnostics.ToImmutable();

    public ReadOnlySpan<char> GetText(in SyntaxToken token)
    {
        return _lexer.SourceText.GetCharacterSpan(token.TextSpan);
    }

    public ReadOnlySpan<char> GetValueText(in SyntaxToken token)
    {
        return _lexer.SourceText.GetCharacterSpan(token.GetValueSpan());
    }

    public Enumerator GetEnumerator() => new(_lexer);

    public SyntaxToken[] RealizeTokens()
    {
        var builder = new ArrayBuilder<SyntaxToken>(32);
        foreach (SyntaxToken tk in this)
        {
            builder.Add() = tk;
        }

        return builder.ToArray();
    }

    public SyntaxToken SingleToken()
    {
        SyntaxToken tk = default;
        foreach (SyntaxToken token in this)
        {
            if (tk.Kind == SyntaxTokenKind.None)
            {
                tk = token;
            }
            else if (token.Kind != SyntaxTokenKind.EndOfFileToken)
            {
                throw new InvalidOperationException("Lexing result contains more than one token.");
            }
        }

        if (tk.Kind == SyntaxTokenKind.None)
        {
            throw new InvalidOperationException("Lexing result contains no tokens.");
        }

        return tk;
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
