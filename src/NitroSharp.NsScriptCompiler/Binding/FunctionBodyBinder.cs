using System.Collections.Generic;
using System.Collections.Immutable;
using NitroSharp.NsScriptNew.Symbols;

namespace NitroSharp.NsScriptNew.Binding
{
    public sealed class FunctionBodyBinder : Binder
    {
        private readonly SourceFileBinder _parent;
        private readonly FunctionSymbol _functionSymbol;
        private readonly Dictionary<string, ParameterSymbol> _parameterMap;

        public FunctionBodyBinder(SourceFileBinder parent, FunctionSymbol functionSymbol) : base(parent)
        {
            _parent = parent;
            _functionSymbol = functionSymbol;
            ImmutableArray<ParameterSymbol> parameters = functionSymbol.Parameters;
            if (parameters.Length > 0)
            {
                _parameterMap = new Dictionary<string, ParameterSymbol>();
                foreach (ParameterSymbol parameter in parameters)
                {
                    _parameterMap[parameter.Name] = parameter;
                }
            }
        }

        internal override Symbol LookupFunction(string name)
        {
            return _parent.LookupFunction(name);
        }

        protected override ParameterSymbol LookupParameter(string name)
        {
            if (_parameterMap != null)
            {
                return _parameterMap.TryGetValue(name, out ParameterSymbol symbol)
                    ? symbol : null;
            }

            return null;
        }
    }
}
