using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp.Experimental
{
    internal readonly struct EntityMove
    {
        public readonly Entity Entity;
        public readonly uint NewIndexInStorage;

        public EntityMove(Entity entity, uint newIndexInStorage)
            => (Entity, NewIndexInStorage) = (entity, newIndexInStorage);

        public bool IsEmpty => !Entity.IsValid;

        public static EntityMove Empty => new EntityMove(Entity.Invalid, 0);
    }

    internal enum StorageArea
    {
        Uninitialized = 0,
        Active = 1,
        Inactive = 2
    }

    internal abstract class EntityHub
    {
        public abstract World World { get; }

        public abstract EntityStorage GetStorage(StorageArea area);
        public abstract ReadOnlySpan<Entity> GetEntities(StorageArea area);
        public abstract void BeginFrame();
    }

    internal sealed class EntityHub<T> : EntityHub where T : EntityStorage
    {
        public EntityHub(World world, Func<EntityHub, T> factory)
        {
            World = world;
            Uninitialized = factory(this);
            Active = factory(this);
            Inactive = factory(this);
        }

        public override World World { get; }
        public T Uninitialized { get; }
        public T Active { get; }
        public T Inactive { get; }

        public override EntityStorage GetStorage(StorageArea area)
        {
            return area switch
            {
                StorageArea.Uninitialized => Uninitialized,
                StorageArea.Active => Active,
                StorageArea.Inactive => Inactive,
                _ => ThrowHelper.Unreachable<EntityStorage>()
            };
        }

        public override ReadOnlySpan<Entity> GetEntities(StorageArea area)
        {
            return area switch
            {
                StorageArea.Uninitialized => Uninitialized.Entities,
                StorageArea.Active => Active.Entities,
                StorageArea.Inactive => Inactive.Entities,
                _ => throw new Exception("Unreachable")
            };
        }

        public override void BeginFrame()
        {
            EntityStorage.MoveEntities(srcStorage: Uninitialized, dstStorage: Active);
        }
    }

    internal abstract class EntityStorage
    {
        internal abstract class ComponentVec
        {
            public abstract void MoveComponents(ComponentVec dst);
            public abstract void Grow(int newSize);
            public abstract void Move(uint srcIndex, uint dstIndex);
            public abstract void Move(uint index, ComponentVec dst);
            public abstract void Erase(uint index);
        }

        internal class ComponentVec<T> : ComponentVec
        {
            private readonly EntityStorage _entityStorage;
            private readonly World _world;
            protected T[] _array;

            public ComponentVec(EntityStorage entityStorage, uint initialCapacity)
            {
                _entityStorage = entityStorage;
                _world = entityStorage.Hub.World;
                _array = new T[initialCapacity];
            }

            public uint Count => _entityStorage.Count;
            public Span<T> All => _array.AsSpan(0, (int)_entityStorage.Count);

            public ref T this[uint index]
            {
                get
                {
                    Debug.Assert(index < _entityStorage.Count);
                    return ref _array[index];
                }
            }

            public ref T this[Entity entity]
            {
                get
                {
                    World.EntityPointer ptr = _world.LookupPointer(entity);
                    Debug.Assert(ReferenceEquals(_entityStorage, ptr.Storage));
                    return ref _array[ptr.IndexInStorage];
                }
            }

            public override void MoveComponents(ComponentVec dst)
            {
                ComponentVec<T> srcStorage = this;
                var dstVec = (ComponentVec<T>)dst;
                ref T[] dstArray = ref dstVec._array;
                int requiredLength = (int)(dstVec.Count + srcStorage.Count);
                if (dstArray.Length < requiredLength)
                {
                    int newSize = MathUtil.RoundUp(requiredLength, multiple: dstArray.Length);
                    Array.Resize(ref dstArray, newSize);
                }

                ReadOnlySpan<T> srcComponents = srcStorage.All;
                Span<T> dstComponents = dstArray.AsSpan((int)dstVec.Count, (int)srcStorage.Count);
                srcComponents.CopyTo(dstComponents);
            }

            public override void Grow(int newSize)
            {
                Array.Resize(ref _array, newSize);
            }

            public override void Move(uint srcIndex, uint dstIndex)
            {
                T[] array = _array;
                ref T src = ref array[srcIndex];
                array[dstIndex] = src;
                src = default;
            }

            public override void Move(uint index, ComponentVec dst)
            {
                var dstVec = (ComponentVec<T>)dst;
                ref T[] dstArray = ref dstVec._array;
                if (dstArray.Length == dstVec.Count)
                {
                    dstVec.Grow(dstArray.Length * 2);
                }

                dstArray[dstVec.Count] = _array[index];
                _array[index] = _array[Count - 1];
                _array[Count - 1] = default;
            }

            public override void Erase(uint index)
            {
                _array[index] = default;
            }
        }

        internal sealed class SystemComponentVec<T> : ComponentVec<T>
        {
            private ArrayBuilder<T> _removedComponents;

            public SystemComponentVec(EntityStorage storage, uint initialCapacity)
                : base(storage, initialCapacity)
            {
                _removedComponents = new ArrayBuilder<T>(initialCapacity / 4);
            }

            public ReadOnlySpan<T> Removed => _removedComponents.AsReadonlySpan();
        }

        protected readonly World _world;
        private readonly uint _initialCapacity;
        private readonly List<ComponentVec> _componentVecs;
        private uint _count;
        private Entity[] _entities;

        protected EntityStorage(EntityHub hub, uint initialCapacity)
        {
            Hub = hub;
            _world = hub.World;
            _initialCapacity = initialCapacity;
            _componentVecs = new List<ComponentVec>();
            _entities = new Entity[initialCapacity];
        }

        public EntityHub Hub { get; }
        public uint Count => _count;
        public ReadOnlySpan<Entity> Entities => _entities.AsSpan(0, (int)_count);

        public (Entity entity, uint index) New(EntityName name)
        {
            Entity e = _world.CreateEntity(name, this);
            uint index = _world.LookupPointer(e).IndexInStorage;
            return (e, index);
        }

        public EntityMove MoveEntity(uint index, EntityStorage dstStorage)
        {
            List<ComponentVec> srcComponentVecs = _componentVecs;
            List<ComponentVec> dstComponentVecs = dstStorage._componentVecs;
            for (int i = 0; i < srcComponentVecs.Count; i++)
            {
                srcComponentVecs[i].Move(index, dstComponentVecs[i]);
            }

            Entity entity = _entities[index];
            EntityMove moveResult = EntityMove.Empty;
            if (index != Count - 1)
            {
                ref Entity lastEntity = ref _entities[Count - 1];
                moveResult = new EntityMove(lastEntity, newIndexInStorage: index);
                _entities[index] = lastEntity;
                lastEntity = Entity.Invalid;
            }
            _count--;
            ref Entity[] dstEntities = ref dstStorage._entities;
            uint requiredLength = dstStorage.Count + 1;
            if (dstEntities.Length < requiredLength)
            {
                Array.Resize(ref dstEntities, dstEntities.Length * 2);
            }
            dstEntities[dstStorage._count++] = entity;
            return moveResult;
        }

        public static void MoveEntities(EntityStorage srcStorage, EntityStorage dstStorage)
        {
            int requiredLength = (int)(dstStorage.Count + srcStorage.Count);
            ref Entity[] dstArray = ref dstStorage._entities;
            if (dstArray.Length < requiredLength)
            {
                int newSize = MathUtil.RoundUp(requiredLength, multiple: dstArray.Length);
                Array.Resize(ref dstArray, newSize);
            }

            ReadOnlySpan<Entity> srcEntities = srcStorage.Entities;
            Span<Entity> dstEntities = dstArray.AsSpan((int)dstStorage.Count, (int)srcStorage.Count);
            srcEntities.CopyTo(dstEntities);

            List<ComponentVec> srcCompStorages = srcStorage._componentVecs;
            List<ComponentVec> dstCompStorages = dstStorage._componentVecs;
            for (int i = 0; i < srcCompStorages.Count; i++)
            {
                srcCompStorages[i].MoveComponents(dstCompStorages[i]);
            }
            dstStorage._count += srcStorage._count;
            srcStorage._count = 0;
        }

        protected ComponentVec<T> AddComponentVec<T>()
        {
            var vec = new ComponentVec<T>(this, _initialCapacity);
            _componentVecs.Add(vec);
            return vec;
        }

        protected SystemComponentVec<T> AddSystemComponentVec<T>()
        {
            var vec = new SystemComponentVec<T>(this, _initialCapacity);
            _componentVecs.Add(vec);
            return vec;
        }

        public uint Insert()
        {
            if (_count == _entities.Length)
            {
                Grow();
            }
            return _count++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow()
        {
            int newSize = _entities.Length * 2;
            Array.Resize(ref _entities, newSize);
            foreach (ComponentVec vec in _componentVecs)
            {
                vec.Grow(newSize);
            }
        }

        private EntityMove Move(uint srcIndex, uint dstIndex)
        {
            foreach (ComponentVec storage in _componentVecs)
            {
                storage.Move(srcIndex, dstIndex);
            }
            ref Entity entity = ref _entities[srcIndex];
            var moveResult = new EntityMove(entity, newIndexInStorage: dstIndex);
            _entities[dstIndex] = entity;
            entity = Entity.Invalid;
            return moveResult;
        }

        public void SetEntity(uint index, Entity entity)
        {
            _entities[index] = entity;
        }

        public EntityMove Remove(uint index)
        {
            uint last = --_count;
            return last != index
                ? Move(srcIndex: last, dstIndex: index)
                : EntityMove.Empty;
        }
    }
}
