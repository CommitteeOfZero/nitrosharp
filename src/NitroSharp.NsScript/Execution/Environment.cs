using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp.NsScript.Execution
{
    public sealed class Environment
    {
        public static readonly Environment Empty = new Environment();

        private readonly Dictionary<string, ConstantValue> _map;

        public Environment()
        {
            _map = new Dictionary<string, ConstantValue>();
        }

        public ConstantValue Get(string identifier)
        {
            if (_map.TryGetValue(identifier, out var value))
            {
                return value;
            }

            return _map[identifier] = ConstantValue.Null;
        }

        public bool TryGetValue(string identifier, out ConstantValue value) => _map.TryGetValue(identifier, out value);
        public bool Contains(string identifier) => _map.ContainsKey(identifier);

        internal void Set(string identifier, ConstantValue newValue)
        {
            Debug.Assert(!ReferenceEquals(newValue, null));
            if (_map.TryGetValue(identifier, out ConstantValue oldValue) && !(oldValue is null))
            {
                if (oldValue.Type != BuiltInType.Null
                    && (oldValue.Type == BuiltInType.String
                    || newValue.Type != BuiltInType.String))
                {
                    newValue.TryConvertTo(oldValue.Type, out newValue);
                }
            }

            _map[identifier] = newValue;
        }
    }
}
