using System.Collections.Generic;

namespace NitroSharp
{
    internal readonly struct ReadOnlyHashSet<T>
    {
        private readonly HashSet<T> _hashSet;

        public ReadOnlyHashSet(HashSet<T> hashSet)
        {
            _hashSet = hashSet;
        }

        public HashSet<T>.Enumerator GetEnumerator() => _hashSet.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => _hashSet.GetEnumerator();
    }
}
