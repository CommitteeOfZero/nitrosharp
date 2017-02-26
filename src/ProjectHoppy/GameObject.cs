using System.Collections.Generic;

namespace ProjectHoppy
{
    public abstract class GameObject
    {
        private List<string> _aliases;

        public GameObject()
        {
            _aliases = new List<string>();
        }

        public void AddAlias(string alias)
        {
            _aliases.Add(alias);
        }
    }
}
