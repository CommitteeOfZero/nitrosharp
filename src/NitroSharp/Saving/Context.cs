using System.Collections.Generic;
using NitroSharp.Content;
using NitroSharp.Graphics;
using NitroSharp.NsScript.VM;
using Veldrid;

namespace NitroSharp.Saving
{
    internal sealed class GameSavingContext
    {
        private readonly List<Texture> _textures = new();

        public GameSavingContext(World world)
        {
            World = world;
        }

        public World World { get; }
        public IReadOnlyList<Texture> StandaloneTextures => _textures;

        public int AddStandaloneTexture(Texture texture)
        {
            int id = _textures.Count;
            _textures.Add(texture);
            return id;
        }
    }

    internal sealed class GameLoadingContext
    {
        public IReadOnlyList<Texture> StandaloneTextures { get; init; }
        public GameContext GameContext { get; init; }
        public RenderContext Rendering { get; init; }
        public ContentManager Content { get; init; }
        public NsScriptVM VM { get; init; }
        public GameProcess Process { get; init; }
        public Backlog Backlog { get; init; }
    }
}
