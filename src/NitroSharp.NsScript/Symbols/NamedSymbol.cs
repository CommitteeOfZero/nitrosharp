namespace NitroSharp.NsScript.Symbols
{
    public abstract class NamedSymbol : Symbol
    {
        protected NamedSymbol(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
