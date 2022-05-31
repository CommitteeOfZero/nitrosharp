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

        public GameSavingContext(World world, RenderContext renderContext)
        {
            World = world;
            RenderContext = renderContext;
        }

        public World World { get; }
        public IReadOnlyList<Texture> StandaloneTextures => _textures;
        public RenderContext RenderContext { get; }

        public int AddStandaloneTexture(Texture texture)
        {
            int id = _textures.Count;
            _textures.Add(texture);
            return id;
        }
    }

    internal sealed class GameLoadingContext
    {
        public IReadOnlyList<Texture> StandaloneTextures { get; init; } = null!;
        public GameContext GameContext { get; init; } = null!;
        public RenderContext Rendering { get; init; } = null!;
        public ContentManager Content { get; init; } = null!;
        public NsScriptVM VM { get; init; } = null!;
        public GameProcess Process { get; init; } = null!;
        public Backlog Backlog { get; init; } = null!;
    }
}
