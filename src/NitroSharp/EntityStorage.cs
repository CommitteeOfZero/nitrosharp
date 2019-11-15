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
        internal abstract class ComponentStorage
        {
            public abstract void MoveComponents(ComponentStorage dstStorage);
            public abstract void Grow(int newSize);
            public abstract void Move(uint srcIndex, uint dstIndex);
            public abstract void Move(uint index, ComponentStorage dstStorage);
            public abstract void Erase(uint index);
        }

        internal class ComponentStorage<T> : ComponentStorage
        {
            private readonly EntityStorage _entityStorage;
            private readonly World _world;
            protected T[] _array;

            public ComponentStorage(EntityStorage entityStorage, uint initialCapacity)
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
                    World.EntityPointer ptr = _world.LookupIndexInStorage(entity);
                    Debug.Assert(ReferenceEquals(_entityStorage, ptr.Storage));
                    return ref _array[ptr.IndexInStorage];
                }
            }

            public override void MoveComponents(ComponentStorage abstractDstStorage)
            {
                ComponentStorage<T> srcStorage = this;
                var dstStorage = (ComponentStorage<T>)abstractDstStorage;
                ref T[] dstArray = ref dstStorage._array;
                int requiredLength = (int)(dstStorage.Count + srcStorage.Count);
                if (dstArray.Length < requiredLength)
                {
                    int newSize = MathUtil.RoundUp(requiredLength, multiple: dstArray.Length);
                    Array.Resize(ref dstArray, newSize);
                }

                ReadOnlySpan<T> src = srcStorage.All;
                Span<T> dst = dstArray.AsSpan((int)dstStorage.Count, (int)srcStorage.Count);
                src.CopyTo(dst);
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

            public override void Move(uint index, ComponentStorage abstractDstStorage)
            {
                ComponentStorage<T> srcStorage = this;
                var dstStorage = (ComponentStorage<T>)abstractDstStorage;
                ref T[] dstArray = ref dstStorage._array;
                if (dstArray.Length == dstStorage.Count)
                {
                    dstStorage.Grow(dstArray.Length * 2);
                }

                dstArray[dstStorage.Count] = _array[index];
                _array[index] = _array[Count - 1];
                _array[Count - 1] = default;
            }

            public override void Erase(uint index)
            {
                _array[index] = default;
            }
        }

        internal sealed class SystemComponentStorage<T> : ComponentStorage<T>
        {
            private ArrayBuilder<T> _removedComponents;

            public SystemComponentStorage(EntityStorage storage, uint initialCapacity)
                : base(storage, initialCapacity)
            {
                _removedComponents = new ArrayBuilder<T>(initialCapacity / 4);
            }

            public ReadOnlySpan<T> Removed => _removedComponents.AsReadonlySpan();
        }

        protected readonly World _world;
        private readonly uint _initialCapacity;
        private readonly List<ComponentStorage> _componentStorages;
        private uint _count;
        private Entity[] _entities;

        protected EntityStorage(EntityHub hub, uint initialCapacity)
        {
            Hub = hub;
            _world = hub.World;
            _initialCapacity = initialCapacity;
            _componentStorages = new List<ComponentStorage>();
            _entities = new Entity[initialCapacity];
        }

        public EntityHub Hub { get; }
        public uint Count => _count;
        public ReadOnlySpan<Entity> Entities => _entities.AsSpan(0, (int)_count);

        public (Entity entity, uint index) New(EntityName name)
        {
            Entity e = _world.CreateEntity(name, this);
            uint index = _world.LookupIndexInStorage(e).IndexInStorage;
            return (e, index);
        }

        public EntityMove MoveEntity(uint index, EntityStorage dstStorage)
        {
            List<ComponentStorage> srcCompStorages = _componentStorages;
            List<ComponentStorage> dstCompStorages = dstStorage._componentStorages;
            for (int i = 0; i < srcCompStorages.Count; i++)
            {
                srcCompStorages[i].Move(index, dstCompStorages[i]);
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

            List<ComponentStorage> srcCompStorages = srcStorage._componentStorages;
            List<ComponentStorage> dstCompStorages = dstStorage._componentStorages;
            for (int i = 0; i < srcCompStorages.Count; i++)
            {
                srcCompStorages[i].MoveComponents(dstCompStorages[i]);
            }
            dstStorage._count += srcStorage._count;
            srcStorage._count = 0;
        }

        protected ComponentStorage<T> AddComponentStorage<T>()
        {
            var storage = new ComponentStorage<T>(this, _initialCapacity);
            _componentStorages.Add(storage);
            return storage;
        }

        protected SystemComponentStorage<T> AddSystemComponentStorage<T>()
        {
            var storage = new SystemComponentStorage<T>(this, _initialCapacity);
            _componentStorages.Add(storage);
            return storage;
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
            foreach (ComponentStorage storage in _componentStorages)
            {
                storage.Grow(newSize);
            }
        }

        private EntityMove Move(uint srcIndex, uint dstIndex)
        {
            foreach (ComponentStorage storage in _componentStorages)
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
