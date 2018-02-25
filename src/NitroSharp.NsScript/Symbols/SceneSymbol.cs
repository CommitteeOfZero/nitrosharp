using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class SceneSymbol : InvocableSymbol
    {
        internal SceneSymbol(string name, Scene declaration) : base(name, declaration)
        {
        }

        public override SymbolKind Kind => SymbolKind.Scene;
        public override string ToString() => $"Scene '{Name}'";
    }
}
