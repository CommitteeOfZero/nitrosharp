using System.Collections.Immutable;

namespace SciAdvNet.NSScript
{
    public sealed class NSSyntaxTree
    {
        internal NSSyntaxTree(Chapter mainChapter, ImmutableArray<Function> functions,
            ImmutableArray<Scene> scenes, ImmutableArray<string> includes)
        {
            MainChapter = mainChapter;
            Functions = functions;
            Scenes = scenes;
            Includes = includes;
        }

        public Chapter MainChapter { get; }
        public ImmutableArray<Function> Functions { get; }
        public ImmutableArray<Scene> Scenes { get; }
        public ImmutableArray<string> Includes { get; }
    }
}
