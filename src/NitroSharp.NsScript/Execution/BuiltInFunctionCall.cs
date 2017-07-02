using System.Collections.Immutable;
using System.Linq;

namespace NitroSharp.NsScript.Execution
{
    public struct BuiltInFunctionCall
    {
        private ImmutableArray<ConstantValue> _immutableArgs;
        internal static BuiltInFunctionCall Empty = new BuiltInFunctionCall();

        internal BuiltInFunctionCall(string functionName, ArgumentStack arguments, ThreadContext callingThread)
        {
            FunctionName = functionName;
            MutableArguments = arguments;
            _immutableArgs = ImmutableArray<ConstantValue>.Empty;
            CallingThread = callingThread;
        }

        public string FunctionName { get; }
        public ImmutableArray<ConstantValue> Arguments
        {
            get
            {
                if (MutableArguments.Count > 0 && _immutableArgs.IsEmpty)
                {
                    _immutableArgs = MutableArguments.Reverse().ToImmutableArray();
                }

                return _immutableArgs;
            }
        }

        internal ArgumentStack MutableArguments { get; }

        public ThreadContext CallingThread { get; }
        internal bool IsEmpty => string.IsNullOrEmpty(FunctionName);

        public override string ToString()
        {
            string args = string.Empty;
            if (MutableArguments.Count > 0)
            {
                args = MutableArguments.Count == 1 ? MutableArguments.First().ToString() : string.Join(", ", MutableArguments);
            }

            return $"{FunctionName}({args})";
        }
    }
}
