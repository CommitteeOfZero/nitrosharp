using System.Collections.Generic;
using System.Collections.Immutable;

namespace SciAdvNet.NSScript.Execution
{
    public class Module
    {
        private Dictionary<string, Method> _methods;

        internal Module(string name, NSSyntaxTree syntaxTree, ImmutableArray<Module> dependencies)
        {
            Name = name;
            SyntaxTree = syntaxTree;
            Dependencies = dependencies;

            ConstructDict();
        }

        public string Name { get; }
        public NSSyntaxTree SyntaxTree { get; }
        public Chapter MainChapter => SyntaxTree.MainChapter;
        public ImmutableArray<Module> Dependencies { get; }

        public Method GetMethod(string name) => _methods[name];
        public bool TryGetMethod(string name, out Method method) => _methods.TryGetValue(name, out method);

        private void ConstructDict()
        {
            _methods = new Dictionary<string, Method>();
            AddMethods(this);
            foreach (var dep in Dependencies)
            {
                AddMethods(dep);
            }
        }

        private void AddMethods(Module module)
        {
            foreach (var method in module.SyntaxTree.Methods)
            {
                _methods[method.Name.SimplifiedName] = method;
            }
        }
    }
}
