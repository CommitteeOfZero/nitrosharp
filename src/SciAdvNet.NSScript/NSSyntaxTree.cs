using System.Collections.Immutable;

namespace SciAdvNet.NSScript
{
    public sealed class NSSyntaxTree
    {
        internal NSSyntaxTree(Chapter mainChapter, ImmutableArray<Method> methods,
            ImmutableArray<Scene> scenes, ImmutableArray<string> includes)
        {
            MainChapter = mainChapter;
            Methods = methods;
            Scenes = scenes;
            Includes = includes;
        }

        public Chapter MainChapter { get; }
        public ImmutableArray<Method> Methods { get; }
        public ImmutableArray<Scene> Scenes { get; }
        public ImmutableArray<string> Includes { get; }
    }
}
