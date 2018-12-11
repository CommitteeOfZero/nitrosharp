using System.Collections.Generic;
using NitroSharp.NsScriptNew.Symbols;

namespace NitroSharp.NsScriptNew.Binding
{
    public sealed class SourceFileBinder : Binder
    {
        private readonly SourceFileSymbol _sourceFileSymbol;
        private readonly Dictionary<MemberSymbol, BoundBlock> _boundMembers;

        public SourceFileBinder(SourceFileSymbol sourceFileSymbol) : base(null)
        {
            _sourceFileSymbol = sourceFileSymbol;
            _boundMembers = new Dictionary<MemberSymbol, BoundBlock>();
        }

        public BoundBlock BindMember(MemberSymbol memberSymbol)
        {
            if (_boundMembers.TryGetValue(memberSymbol, out BoundBlock block))
            {
                return block;
            }

            if (memberSymbol is FunctionSymbol function)
            {
                var binder = new FunctionBodyBinder(this, function);
                block = binder.BindBlock(function.Declaration.Body);
            }
            else
            {
                block = BindBlock(memberSymbol.Declaration.Body);
            }

            _boundMembers[memberSymbol] = block;
            return block;
        }

        internal override Symbol LookupFunction(string name)
        {
            BuiltInFunctionSymbol builtInFunction = GlobalBinder.LookupBuiltInFunction(name);
            return (Symbol)builtInFunction ?? _sourceFileSymbol.Module.LookupFunction(name);
        }
    }
}
