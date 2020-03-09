using System.Diagnostics;
using NitroSharp.Experimental;
using NitroSharp.Interactivity;

#nullable enable

namespace NitroSharp
{
    internal sealed class ThreadRecordStorage : EntityStorage
    {
        public ComponentVec<InterpreterThreadInfo> Infos { get; }

        public (Entity entity, uint index) New(
            EntityName name,
            in InterpreterThreadInfo info)
        {
            (Entity e, uint i) = base.New(name);
            Infos[i] = info;
            if (name.MouseState.HasValue)
            {
                Debug.Assert(name.Parent != null);
                Entity choice = _world.GetEntity(new EntityName(name.Parent));
                ChoiceStorage? choices = _world.GetStorage<ChoiceStorage>(choice);
                if (choices != null)
                {
                    choices.AssociatedEntities[choice]
                        .SetThreadEntity(name.MouseState.Value, e);
                }
            }
            return (e, i);
        }

        public ThreadRecordStorage(EntityHub hub, uint initialCapacity)
            : base(hub, initialCapacity)
        {
            Infos = AddComponentVec<InterpreterThreadInfo>();
        }
    }

    internal readonly struct InterpreterThreadInfo
    {
        public InterpreterThreadInfo(string name, string module, string target)
        {
            Name = name;
            Module = module;
            Target = target;
        }

        public readonly string Name;
        public readonly string Module;
        public readonly string Target;
    }
}
