using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public sealed class DialogueBlockSymbol : InvocableSymbol
    {
        private readonly DialogueBlock _block;

        internal DialogueBlockSymbol(string name, DialogueBlock declaration) : base(name, declaration)
        {
            _block = declaration;
        }

        public override SymbolKind Kind => SymbolKind.DialogueBlock;

        public string AssociatedBox => _block.AssociatedBox;
        public string Identifier => _block.Identifier.Name;
    }
}
