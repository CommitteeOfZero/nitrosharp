using System;

namespace NitroSharp.NsScript
{
    public sealed class NsParseException : Exception
    {
        public NsParseException()
        {
        }

        public NsParseException(string message)
            : base(message)
        {
        }

        public NsParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NsParseException(string message, string scriptName, Exception innerException)
            : base(message, innerException)
        {
            ScriptName = scriptName;
        }

        public NsParseException(string message, string scriptName)
            : base(message)
        {
            ScriptName = scriptName;
        }

        public string ScriptName { get; }
    }

    public sealed class NsRuntimeErrorException : Exception
    {
        public NsRuntimeErrorException()
        {
        }

        public NsRuntimeErrorException(string message)
            : base(message)
        {
        }

        public NsRuntimeErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NsRuntimeErrorException(string message, string faultyModule, Exception innerException)
            : base(message, innerException)
        {
            FaultyModule = faultyModule;
        }

        public NsRuntimeErrorException(string message, string faultyModule)
            : base(message)
        {
            FaultyModule = faultyModule;
        }

        public string FaultyModule { get; }
    }
}
