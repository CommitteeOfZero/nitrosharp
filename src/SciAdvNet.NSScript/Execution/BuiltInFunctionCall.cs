using System.Collections.Immutable;
using System.Linq;

namespace SciAdvNet.NSScript.Execution
{
    public struct BuiltInFunctionCall
    {
        private ImmutableArray<ConstantValue> _immutableArgs;
        internal static BuiltInFunctionCall Empty = new BuiltInFunctionCall();

        internal BuiltInFunctionCall(string functionName, ArgumentStack arguments, uint callingThreadId)
        {
            FunctionName = functionName;
            MutableArguments = arguments;
            _immutableArgs = ImmutableArray<ConstantValue>.Empty;
            CallingThreadId = callingThreadId;
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

        public uint CallingThreadId { get; }
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
