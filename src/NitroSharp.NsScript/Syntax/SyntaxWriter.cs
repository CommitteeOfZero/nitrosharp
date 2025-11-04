using System.IO;

namespace NitroSharp.NsScript.Syntax;

public abstract class SyntaxWriter(TextWriter textWriter) : SyntaxVisitor
{
    private int _indent;
    private bool _writeIndent = true;

    protected void Write(string text)
    {
        if (_writeIndent)
        {
            WriteIndent();
        }

        textWriter.Write(text);
        _writeIndent = false;
    }

    protected void WriteLine()
    {
        textWriter.WriteLine();
        _writeIndent = true;
    }

    protected void WriteLine(string text)
    {
        if (_writeIndent)
        {
            WriteIndent();
        }

        textWriter.WriteLine(text);
        _writeIndent = true;
    }

    protected void Write(Spanned<string> spannedString)
    {
        Write(spannedString.Value);
    }

    protected void WriteSpace()
    {
        Write(" ");
    }

    protected void Indent()
    {
        _indent++;
    }

    protected void Outdent()
    {
        if (_indent > 0)
        {
            _indent--;
        }
    }

    protected void WriteIndent()
    {
        for (int i = 0; i < _indent; i++)
        {
            textWriter.Write("    ");
        }

        _writeIndent = false;
    }
}
