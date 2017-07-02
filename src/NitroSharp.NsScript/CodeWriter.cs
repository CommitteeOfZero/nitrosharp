using System.IO;

namespace NitroSharp.NsScript
{
    public abstract class CodeWriter : SyntaxVisitor
    {
        private readonly TextWriter _writer;
        private bool _writeIndent;
        private int _indent;

        protected CodeWriter(TextWriter textWriter)
        {
            _writer = textWriter;
        }

        public void WriteNode(SyntaxNode node)
        {
            Visit(node);
        }

        protected void Write(string str)
        {
            if (_writeIndent)
            {
                WriteIndent();
            }

            _writer.Write(str);
            _writeIndent = false;
        }

        protected void WriteSpace()
        {
            _writer.Write(" ");
        }

        protected void WriteLine()
        {
            _writer.WriteLine();
            _writeIndent = true;
        }

        protected void Indent()
        {
            _indent++;
        }

        protected void Outdent()
        {
            _indent--;
        }

        private void WriteIndent()
        {
            for (int i = 0; i < _indent; i++)
            {
                _writer.Write("\t");
            }
        }
    }
}
