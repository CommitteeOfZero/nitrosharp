using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy.Core
{
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = Comparer<TKey>.Default.Compare(x, y);
            return result == 0 ? -1 : result;
        }
    }
}
