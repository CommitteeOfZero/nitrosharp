using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public abstract class SourceSymbol : NamedSymbol
    {
        protected SourceSymbol(string name, Declaration declaration) : base(name)
        {
            Declaration = declaration;
        }

        public Declaration Declaration { get; }
    }
}
