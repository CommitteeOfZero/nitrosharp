using System.Collections.Immutable;

namespace NitroSharp.NsScript
{
    public sealed class NsSyntaxTree
    {
        internal NsSyntaxTree(Chapter mainChapter, ImmutableArray<Function> functions,
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
