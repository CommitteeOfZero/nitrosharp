using NitroSharp.NsScript.Text;
using System.Globalization;

namespace NitroSharp.NsScript
{
    public class Diagnostic
    {
        public static Diagnostic Create(TextSpan span, DiagnosticId id) => new Diagnostic(span, id);
        public static Diagnostic Create(TextSpan span, DiagnosticId id, params object[] arguments)
            => new DiagnosticWithArguments(span, id, arguments);

        private Diagnostic(TextSpan span, DiagnosticId id)
        {
            Span = span;
            Id = id;
        }

        public DiagnosticId Id { get; }
        public TextSpan Span { get; }
        public virtual string Message => DiagnosticInfo.GetMessage(Id);
        public DiagnosticSeverity Severity => DiagnosticInfo.GetSeverity(Id);

        private sealed class DiagnosticWithArguments : Diagnostic
        {
            private readonly object[] _arguments;
            private string _message;

            public DiagnosticWithArguments(TextSpan span, DiagnosticId id, params object[] arguments) : base(span, id)
            {
                _arguments = arguments;
            }

            public override string Message
            {
                get
                {
                    if (_message == null)
                    {
                        _message = FormatMessage();
                    }

                    return _message;
                }
            }

            private string FormatMessage()
            {
                string formatString = DiagnosticInfo.GetMessage(Id);
                return string.Format(CultureInfo.CurrentCulture, formatString, _arguments);
            }
        }
    }
}
