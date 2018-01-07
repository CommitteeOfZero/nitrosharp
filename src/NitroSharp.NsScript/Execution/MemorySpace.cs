using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp.NsScript.Execution
{
    public sealed class MemorySpace
    {
        public static readonly MemorySpace Empty = new MemorySpace();

        private readonly Dictionary<string, ConstantValue> _map;

        public MemorySpace()
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

        public void Set(string identifier, ConstantValue value)
        {
            Debug.Assert(!ReferenceEquals(value, null));
            _map[identifier] = value;
        }
    }
}
