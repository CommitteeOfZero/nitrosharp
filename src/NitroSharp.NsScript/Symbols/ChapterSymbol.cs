using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class ChapterSymbol : InvocableSymbol
    {
        internal ChapterSymbol(string name, Chapter declaration) : base(name, declaration)
        {
        }

        public override SymbolKind Kind => SymbolKind.Chapter;

        public override string ToString() => $"Chapter '{Name}'";
    }
}
