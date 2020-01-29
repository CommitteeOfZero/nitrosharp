using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Graphics;
using NitroSharp.Interactivity;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp.Experimental
{
    internal readonly struct Entity : IEquatable<Entity>
    {
        public readonly uint Index;
        public readonly uint Version;

        public Entity(uint index, uint verison)
            => (Index, Version) = (index, verison);

        public static Entity Invalid => default;

        public bool IsValid => Version > 0;

        public bool Equals(Entity other)
            => Index == other.Index && Version == other.Version;

        public override int GetHashCode()
            => HashCode.Combine(Index, Version);
    }

    internal sealed class World
    {
        public struct EntityPointer
        {
            public EntityStorage? Storage;
            public uint IndexInStorage;
        }

        private const uint InitialVersion = 1;
        private const uint None = uint.MaxValue;

        private EntityPointer[] _entityPointers;
        private uint[] _versionByEntity;
        private Entity[] _parentByEntity;
        private SmallList<Entity>[] _childrenByEntity;
        private SmallList<EntityName>[] _childrenNamesByEntity;
        private bool[] _isLockedByEntity;

        private uint _entityCount;
        private uint _nextFreeSlot;

        private readonly Dictionary<EntityName, Entity> _entities;
        private readonly Dictionary<EntityName, EntityName> _aliases;
        private readonly List<EntityHub> _entityHubs;

        private readonly Dictionary<AnimationDictionaryKey, PropertyAnimation> _activeAnimations;
        private readonly List<(AnimationDictionaryKey key, PropertyAnimation anim)> _animationsToDeactivate;
        private readonly List<(AnimationDictionaryKey, PropertyAnimation)> _queuedAnimations;

        private readonly List<(Entity entity, bool enable)> _entitiesToEnable;

        public World()
        {
            const int capacity = 1024;
            _entityPointers = new EntityPointer[capacity];
            _versionByEntity = new uint[capacity];
            _parentByEntity = new Entity[capacity];
            _childrenByEntity = new SmallList<Entity>[capacity];
            _childrenNamesByEntity = new SmallList<EntityName>[capacity];
            _isLockedByEntity = new bool[capacity];
            _nextFreeSlot = None;
            _entities = new Dictionary<EntityName, Entity>();
            _aliases = new Dictionary<EntityName, EntityName>();

            _activeAnimations = new Dictionary<AnimationDictionaryKey, PropertyAnimation>();
            _animationsToDeactivate = new List<(AnimationDictionaryKey key, PropertyAnimation anim)>();
            _queuedAnimations = new List<(AnimationDictionaryKey, PropertyAnimation)>();
            _entitiesToEnable = new List<(Entity entity, bool enable)>();

            _entityHubs = new List<EntityHub>();
            Quads = AddHub(hub => new QuadStorage(hub, 512));
            Images = AddHub(hub => new ImageStorage(hub, 512));
            AlphaMasks = AddHub(hub => new AlphaMaskStorage(hub, 4));
            TextBlocks = AddHub(hub => new TextBlockStorage(hub, 16));
            ThreadRecords = AddHub(hub => new ThreadRecordStorage(hub, 16));
            Choices = AddHub(hub => new ChoiceStorage(hub, 16));
        }

        public Dictionary<EntityName, Entity>.Enumerator EntityEnumerator => _entities.GetEnumerator();
        public Dictionary<AnimationDictionaryKey, PropertyAnimation>.ValueCollection AttachedAnimations
            => _activeAnimations.Values;

        public EntityHub<QuadStorage> Quads { get; }
        public EntityHub<ImageStorage> Images { get; }
        public EntityHub<AlphaMaskStorage> AlphaMasks { get; }
        public EntityHub<TextBlockStorage> TextBlocks { get; }
        public EntityHub<ThreadRecordStorage> ThreadRecords { get; }
        public EntityHub<ChoiceStorage> Choices { get; }

        private EntityHub<T> AddHub<T>(Func<EntityHub, T> factory) where T : EntityStorage
        {
            var hub = new EntityHub<T>(this, factory);
            _entityHubs.Add(hub);
            return hub;
        }

        public void BeginFrame()
        {
            foreach ((AnimationDictionaryKey key, PropertyAnimation anim) in _queuedAnimations)
            {
                _activeAnimations[key] = anim;
            }
            _queuedAnimations.Clear();

            foreach (EntityHub hub in _entityHubs)
            {
                ReadOnlySpan<Entity> activeEntities = hub.GetEntities(StorageArea.Active);
                ReadOnlySpan<Entity> uninitialized = hub.GetEntities(StorageArea.Uninitialized);
                int dstIndexStart = activeEntities.Length;
                for (int i = 0; i < uninitialized.Length; i++)
                {
                    Entity entity = uninitialized[i];
                    ref EntityPointer ptr = ref _entityPointers[entity.Index];
                    ptr.Storage = hub.GetStorage(StorageArea.Active);
                    ptr.IndexInStorage = (uint)(dstIndexStart + i);
                }
                hub.BeginFrame();
            }

            foreach ((Entity entity, bool enable) in _entitiesToEnable)
            {
                if (Exists(entity))
                {
                    ToggleEnableEntity(entity, enable);
                }
            }
            _entitiesToEnable.Clear();
        }

        public Entity CreateEntity(EntityName name, EntityStorage storage)
        {
            Entity entity = CreateEntityCore(storage);
            _entities[name] = entity;
            if (name.Parent != null)
            {
                var parentName = new EntityName(name.Parent);
                if (_entities.TryGetValue(parentName, out Entity parent))
                {
                    _parentByEntity[entity.Index] = parent;
                    _childrenByEntity[parent.Index].Add(entity);
                    _childrenNamesByEntity[parent.Index].Add(name);
                }
            }
            return entity;
        }

        private Entity CreateEntityCore(EntityStorage storage)
        {
            EnsureCapacity();
            uint freeSlot = _nextFreeSlot;
            uint entityCount = _entityCount;
            uint index = freeSlot != None ? freeSlot : entityCount;
            ref EntityPointer ptr = ref _entityPointers[index];
            ref uint version = ref _versionByEntity[index];
            if (freeSlot != None)
            {
                _nextFreeSlot = ptr.IndexInStorage;
            }
            else
            {
                version = InitialVersion;
            }
            ptr.Storage = storage;
            uint indexInStorage = storage.Insert();
            ptr.IndexInStorage = indexInStorage;
            var entity = new Entity(index, version);
            storage.SetEntity(ptr.IndexInStorage, entity);
            _entityCount++;
            return entity;
        }

        public Entity GetEntity(EntityName name)
            => _entities.TryGetValue(name, out Entity entity)
                ? entity : Entity.Invalid;

        public bool TryGetEntity(EntityName name, out Entity entity)
            => _entities.TryGetValue(name, out entity);

        public T GetStorage<T>(Entity entity) where T : class
        {
            EnsureExists(entity);
            return _entityPointers[entity.Index].Storage as T;
        }

        public EntityPointer LookupPointer(Entity entity)
        {
            EnsureExists(entity);
            return _entityPointers[entity.Index];
        }

        public bool Exists(Entity entity)
        {
            return _versionByEntity[entity.Index] == entity.Version;
        }

        public bool IsActive(Entity entity)
        {
            EntityPointer ptr = LookupPointer(entity);
            Debug.Assert(ptr.Storage != null);
            EntityStorage active = ptr.Storage.Hub.GetStorage(StorageArea.Active);
            return ReferenceEquals(active, ptr.Storage);
        }

        public Entity GetParent(Entity entity)
        {
            EnsureExists(entity);
            return _parentByEntity[entity.Index];
        }

        public void SetParent(Entity entity, Entity parent)
        {
            EnsureExists(entity);
            EnsureExists(parent);
            _parentByEntity[entity.Index] = parent;
        }

        public ReadOnlySpan<Entity> GetChildren(Entity entity)
        {
            EnsureExists(entity);
            return _childrenByEntity[entity.Index].Enumerate();
        }

        public void SetAlias(EntityName name, EntityName alias)
        {
            if (TryGetEntity(name, out Entity entity) || TryGetEntity(alias, out entity))
            {
                _entities[name] = entity;
                _entities[alias] = entity;
                _aliases[name] = alias;
                _aliases[alias] = name;
            }
        }

        public void LockEntity(Entity entity)
        {
            EnsureExists(entity);
            _isLockedByEntity[entity.Index] = true;
        }

        public void UnlockEntity(Entity entity)
        {
            EnsureExists(entity);
            _isLockedByEntity[entity.Index] = false;
        }

        public bool IsLocked(Entity entity)
        {
            EnsureExists(entity);
            return _isLockedByEntity[entity.Index];
        }

        public void ScheduleEnableEntity(Entity entity)
            => _entitiesToEnable.Add((entity, enable: true));

        public void ScheduleDisableEntity(Entity entity)
            => _entitiesToEnable.Add((entity, enable: false));

        public void EnableEntity(Entity entity)
           => ToggleEnableEntity(entity, enable: true);

        public void DisableEntity(Entity entity)
            => ToggleEnableEntity(entity, enable: false);

        private void ToggleEnableEntity(Entity entity, bool enable)
        {
            EnsureExists(entity);
            ref EntityPointer ptr = ref _entityPointers[entity.Index];
            Debug.Assert(ptr.Storage != null);
            EntityHub hub = ptr.Storage.Hub;
            EntityStorage uninitializedStorage = hub.GetStorage(StorageArea.Uninitialized);
            if (ReferenceEquals(ptr.Storage, uninitializedStorage))
            {
                throw new InvalidOperationException("Cannot enable or disable an uninitialized entity.");
            }
            EntityStorage activeStorage = hub.GetStorage(StorageArea.Active);
            EntityStorage inactiveStorage = hub.GetStorage(StorageArea.Inactive);
            bool enabled = ReferenceEquals(ptr.Storage, activeStorage);
            if (enabled != enable)
            {
                (EntityStorage src, EntityStorage dst) = (enabled, enable) switch
                {
                    (true, false) => (activeStorage, inactiveStorage),
                    (false, true) => (inactiveStorage, activeStorage),
                    _ => throw new Exception("Unreachable")
                };
                EntityMove move = src.MoveEntity(ptr.IndexInStorage, dst);
                FixPointer(move);
                ptr.Storage = dst;
                ptr.IndexInStorage = dst.Count - 1;
            }
        }

        public void DestroyEntity(EntityName name)
        {
            if (TryGetEntity(name, out Entity entity))
            {
                DestroyEntity(entity);
                if (_aliases.TryGetValue(name, out EntityName alias))
                {
                    _entities.Remove(alias);
                    _aliases.Remove(alias);
                }
                _entities.Remove(name);
                _aliases.Remove(name);
            }
        }

        private void DestroyEntity(Entity entity)
        {
            EnsureExists(entity);

            SmallList<EntityName> children = _childrenNamesByEntity[entity.Index];
            if (children.Count > 0)
            {
                foreach (EntityName child in children.Enumerate())
                {
                    DestroyEntity(child);
                }
            }

            ref EntityPointer ptr = ref _entityPointers[entity.Index];
            uint indexInStorage = ptr.IndexInStorage;
            Debug.Assert(ptr.Storage != null);
            EntityStorage storage = ptr.Storage;
            ptr.Storage = null;
            ptr.IndexInStorage = _nextFreeSlot;
            _versionByEntity[entity.Index]++;
            _parentByEntity[entity.Index] = Entity.Invalid;
            _childrenByEntity[entity.Index] = default;
            _childrenNamesByEntity[entity.Index] = default;
            _isLockedByEntity[entity.Index] = false;
            _nextFreeSlot = entity.Index;

            EntityMove movedEntity = storage.Remove(indexInStorage);
            FixPointer(movedEntity);
        }

        public bool TryGetAnimation<T>(Entity entity, out T? animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(entity, typeof(T));
            bool result = _activeAnimations.TryGetValue(key, out PropertyAnimation? val);
            animation = val as T;
            return result;
        }

        public void ActivateAnimation<T>(T animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(animation.Entity, typeof(T));
            _queuedAnimations.Add((key, animation));
        }

        public void DeactivateAnimation(PropertyAnimation animation)
        {
            var key = new AnimationDictionaryKey(animation.Entity, animation.GetType());
            _animationsToDeactivate.Add((key, animation));
        }

        public void FlushDetachedAnimations()
        {
            foreach ((AnimationDictionaryKey dictKey, PropertyAnimation anim) in _animationsToDeactivate)
            {
                if (_activeAnimations.TryGetValue(dictKey, out PropertyAnimation? value) && value == anim)
                {
                    _activeAnimations.Remove(dictKey);
                }
            }
            _animationsToDeactivate.Clear();
        }

        private void EnsureCapacity()
        {
            int length = _entityPointers.Length;
            if (length == _entityCount)
            {
                int newLength = length * 2;
                Array.Resize(ref _entityPointers, newLength);
                Array.Resize(ref _versionByEntity, newLength);
                Array.Resize(ref _parentByEntity, newLength);
                Array.Resize(ref _childrenByEntity, newLength);
                Array.Resize(ref _childrenNamesByEntity, newLength);
                Array.Resize(ref _isLockedByEntity, newLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureExists(in Entity entity)
        {
            static void invalid() => throw new InvalidOperationException(
                "Entity does not exist."
            );

            if (entity.Version != _versionByEntity[entity.Index])
            {
                invalid();
            }
        }

        private void FixPointer(EntityMove movedEntity)
        {
            if (!movedEntity.IsEmpty)
            {
                _entityPointers[movedEntity.Entity.Index]
                    .IndexInStorage = movedEntity.NewIndexInStorage;
            }
        }

        internal readonly struct AnimationDictionaryKey : IEquatable<AnimationDictionaryKey>
        {
            public readonly Entity Entity;
            public readonly Type RuntimeType;

            public AnimationDictionaryKey(Entity entity, Type runtimeType)
            {
                Entity = entity;
                RuntimeType = runtimeType;
            }

            public bool Equals(AnimationDictionaryKey other)
                => Entity.Equals(other.Entity) && RuntimeType == other.RuntimeType;

            public override bool Equals(object? obj)
                => obj is AnimationDictionaryKey other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(Entity, RuntimeType);
        }
    }
}
