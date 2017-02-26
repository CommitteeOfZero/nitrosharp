using System.Collections.Immutable;
using System.Linq;

namespace SciAdvNet.NSScript.Execution
{
    public struct BuiltInMethodCall
    {
        private ImmutableArray<ConstantValue> _immutableArgs;
        internal static BuiltInMethodCall Empty = new BuiltInMethodCall();

        internal BuiltInMethodCall(string methodName, ArgumentStack arguments)
        {
            MethodName = methodName;
            MutableArguments = arguments;
            _immutableArgs = ImmutableArray<ConstantValue>.Empty;
        }

        public string MethodName { get; }
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

        internal bool IsEmpty => string.IsNullOrEmpty(MethodName);

        public override string ToString()
        {
            string args = string.Empty;
            if (MutableArguments.Count > 0)
            {
                args = MutableArguments.Count == 1 ? MutableArguments.First().ToString() : string.Join(", ", MutableArguments);
            }

            return $"{MethodName}({args})";
        }
    }
}
