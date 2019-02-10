using System;

namespace NitroSharp.NsScriptNew.VM
{
    public sealed class NsxCallDispatchException : Exception
    {
        public NsxCallDispatchException(
            int invalidArgumentIndex, BuiltInType expectedArgType, BuiltInType actualArgType)
            : base("Error while dispatching a built-in function call: unexpected argument type.")
        {
            InvalidArgumentIndex = invalidArgumentIndex;
            ExpectedArgumentType = expectedArgType;
            ActualArgumentType = actualArgType;
        }

        public int InvalidArgumentIndex { get; }
        public BuiltInType ExpectedArgumentType { get; }
        public BuiltInType ActualArgumentType { get; }
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
