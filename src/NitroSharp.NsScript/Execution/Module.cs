using System.Collections.Generic;
using System.Collections.Immutable;

namespace NitroSharp.NsScript.Execution
{
    public class Module
    {
        private Dictionary<string, Function> _functions;

        internal Module(string name, NsSyntaxTree syntaxTree, ImmutableArray<Module> dependencies)
        {
            Name = name;
            SyntaxTree = syntaxTree;
            Dependencies = dependencies;

            ConstructDict();
        }

        public string Name { get; }
        public NsSyntaxTree SyntaxTree { get; }
        public Chapter MainChapter => SyntaxTree.MainChapter;
        public ImmutableArray<Module> Dependencies { get; }

        public Function GetFunction(string name) => _functions[name];
        public bool TryGetFunction(string name, out Function function) => _functions.TryGetValue(name, out function);

        private void ConstructDict()
        {
            _functions = new Dictionary<string, Function>();
            AddFunctions(this);
            foreach (var dep in Dependencies)
            {
                AddFunctions(dep);
            }
        }

        private void AddFunctions(Module module)
        {
            foreach (var function in module.SyntaxTree.Functions)
            {
                _functions[function.Name.SimplifiedName] = function;
            }
        }
    }
}
