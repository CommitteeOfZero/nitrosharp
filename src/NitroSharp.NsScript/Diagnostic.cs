using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript.Syntax
{
    public sealed class Diagnostic
    {
        public Diagnostic(TextSpan textSpan, DiagnosticId id, string message)
        {
            TextSpan = textSpan;
            Id = id;
            Message = message;
        }

        public DiagnosticId Id { get; }
        public TextSpan TextSpan { get; }
        public string Message { get; }
    }
}
