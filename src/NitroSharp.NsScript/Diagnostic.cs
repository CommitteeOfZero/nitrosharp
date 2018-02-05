using NitroSharp.NsScript.Text;

namespace NitroSharp.NsScript
{
    public sealed class Diagnostic
    {
        public Diagnostic(TextSpan textSpan, DiagnosticId id)
        {
            TextSpan = textSpan;
            Id = id;
        }

        public DiagnosticId Id { get; }
        public TextSpan TextSpan { get; }

        public DiagnosticSeverity Severity => DiagnosticInfo.GetSeverity(Id);
    }
}
