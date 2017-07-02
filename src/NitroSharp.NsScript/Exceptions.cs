using System;

namespace NitroSharp.NsScript
{
    public sealed class NssParseException : Exception
    {
        public NssParseException()
        {
        }

        public NssParseException(string message)
            : base(message)
        {
        }

        public NssParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NssParseException(string message, string scriptName, Exception innerException)
            : base(message, innerException)
        {
            ScriptName = scriptName;
        }

        public NssParseException(string message, string scriptName)
            : base(message)
        {
            ScriptName = scriptName;
        }

        public string ScriptName { get; }
    }

    public sealed class NssRuntimeErrorException : Exception
    {
        public NssRuntimeErrorException()
        {
        }

        public NssRuntimeErrorException(string message)
            : base(message)
        {
        }

        public NssRuntimeErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public NssRuntimeErrorException(string message, string faultyModule, Exception innerException)
            : base(message, innerException)
        {
            FaultyModule = faultyModule;
        }

        public NssRuntimeErrorException(string message, string faultyModule)
            : base(message)
        {
            FaultyModule = faultyModule;
        }

        public string FaultyModule { get; }
    }
}
