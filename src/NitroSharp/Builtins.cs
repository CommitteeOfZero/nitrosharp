using System;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;

namespace NitroSharp.New
{
    internal sealed class Builtins : BuiltInFunctions
    {
        private readonly World _world;

        public Builtins(Game game, World world)
        {
            _world = world;
        }

        public override void CreateEntity(in EntityPath path)
        {
            if (_world.ResolvePath(path, out ResolvedEntityPath resolvedPath))
            {
                var entity = new RenderItem(resolvedPath);
                _world.AddRenderItem(entity);
            }
        }

        public override void WaitForInput()
        {
            VM.SuspendThread(VM.CurrentThread!);
        }
    }
}
