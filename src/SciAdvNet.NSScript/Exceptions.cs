using System;

namespace SciAdvNet.NSScript
{
    internal static class ExceptionUtilities
    {
        public static NssParseException UnexpectedToken(string scriptName, string token)
        {
            return new NssParseException($"Parsing '{scriptName}' failed: unexpected token '{token}'");
        }

        public static NssRuntimeErrorException RuntimeError(string faultyModule, string message, Exception innerException)
        {
            string text = $"An error occured while executing '{faultyModule}': {message}";
            return new NssRuntimeErrorException(text, faultyModule, innerException);
        }
    }

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
