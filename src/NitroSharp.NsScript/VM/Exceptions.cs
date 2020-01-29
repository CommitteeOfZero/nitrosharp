using System;

namespace NitroSharp.NsScript.VM
{
    public sealed class NsxCallDispatchException : Exception
    {
        public NsxCallDispatchException(
            int invalidArgumentIndex, BuiltInType providedArgType)
            : base("Error while dispatching a built-in function call: unexpected argument type.")
        {
            InvalidArgumentIndex = invalidArgumentIndex;
            ProvidedArgumentType = providedArgType;
        }

        public int InvalidArgumentIndex { get; }
        public BuiltInType ProvidedArgumentType { get; }
    }

    public sealed class NsxRuntimeException : Exception
    {
        public NsxRuntimeException()
        {
        }

        public NsxRuntimeException(string message) : base(message)
        {
        }

        public NsxRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
