using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommitteeOfZero.NsScript.Execution
{
    public sealed class VariableTable : IDictionary<string, ConstantValue>
    {
        private readonly Dictionary<string, ConstantValue> _underlyingDictionary;

        public VariableTable()
        {
            _underlyingDictionary = new Dictionary<string, ConstantValue>(StringComparer.OrdinalIgnoreCase);
        }

        public ConstantValue this[string key]
        {
            get
            {
                return Get(key);
            }

            set
            {
                _underlyingDictionary[key] = value;
            }
        }

        private ConstantValue Get(string key)
        {
            ConstantValue value;
            if (TryGetValue(key, out value))
            {
                return value;
            }

            return ConstantValue.Null;
        }

        public int Count => _underlyingDictionary.Count;
        public bool IsReadOnly => false;
        public ICollection<string> Keys => _underlyingDictionary.Keys;
        public ICollection<ConstantValue> Values => _underlyingDictionary.Values;
        public void Add(KeyValuePair<string, ConstantValue> item) => _underlyingDictionary.Add(item.Key, item.Value);
        public void Add(string key, ConstantValue value) => _underlyingDictionary.Add(key, value);
        public void Clear() => _underlyingDictionary.Clear();
        public bool Contains(KeyValuePair<string, ConstantValue> item) => _underlyingDictionary.Contains(item);
        public bool ContainsKey(string key) => _underlyingDictionary.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, ConstantValue>[] array, int arrayIndex) { }
        public IEnumerator<KeyValuePair<string, ConstantValue>> GetEnumerator() => _underlyingDictionary.GetEnumerator();
        public bool Remove(KeyValuePair<string, ConstantValue> item) => _underlyingDictionary.Remove(item.Key);
        public bool Remove(string key) => _underlyingDictionary.Remove(key);
        public bool TryGetValue(string key, out ConstantValue value) => _underlyingDictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _underlyingDictionary.GetEnumerator();
    }
}
