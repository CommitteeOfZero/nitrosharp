using NitroSharp.NsScript.Syntax;

namespace NitroSharp.NsScript.Symbols
{
    public abstract class InvocableSymbol : SourceSymbol
    {
        protected InvocableSymbol(string name, MemberDeclaration declaration) : base(name, declaration)
        {
        }
    }
}
