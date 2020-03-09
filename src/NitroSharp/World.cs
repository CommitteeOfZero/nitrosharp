using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp.New
{
    internal readonly struct ResolvedEntityPath
    {
        public readonly EntityId Id;
        public readonly EntityId ParentId;

        public ResolvedEntityPath(in EntityId id, in EntityId parentId)
            => (Id, ParentId) = (id, parentId);
    }

    internal enum EntityBucket
    {
        Inactive,
        Active
    }

    internal readonly struct EntityLocation
    {
        public readonly EntityBucket Bucket;
        public readonly uint Index;

        public EntityLocation(EntityBucket bucket, uint index)
            => (Bucket, Index) = (bucket, index);
    }

    internal readonly struct EntityMove
    {
        public readonly Entity? Entity;
        public readonly EntityLocation NewLocation;

        public EntityMove(Entity? entity, EntityLocation newLocation)
            => (Entity, NewLocation) = (entity, newLocation);

        public static EntityMove Empty => default;
    }

    internal readonly struct BucketChangeResult
    {
        public readonly EntityLocation NewLocation;
        public readonly EntityMove Swap;

        public BucketChangeResult(EntityLocation newLocation, EntityMove swap)
            => (NewLocation, Swap) = (newLocation, swap);
    }

    internal abstract class EntityGroup
    {
        public abstract BucketChangeResult ChangeBucket(
            EntityLocation entityLocation,
            EntityBucket dstBucket
        );

        public abstract EntityMove Remove(EntityLocation location);
    }

    internal readonly struct EntityGroupView<T>
        where T : Entity
    {
        private readonly EntityGroup<T> _group;

        public EntityGroupView(EntityGroup<T> group)
        {
            _group = group;
        }

        public ReadOnlySpan<T> Enabled => _group.Enabled;
        public ReadOnlySpan<T> Disabled => _group.Disabled;

        public static implicit operator EntityGroupView<T>(EntityGroup<T> group)
            => new EntityGroupView<T>(group);
    }

    internal class EntityGroup<T> : EntityGroup
        where T : Entity
    {
        protected ArrayBuilder<T> _enabledEntities;
        private ArrayBuilder<T> _disabledEntities;

        public EntityGroup(uint nbEnabled = 4, uint nbDisabled = 4)
        {
            _enabledEntities = new ArrayBuilder<T>(nbEnabled);
            _disabledEntities = new ArrayBuilder<T>(nbDisabled);
        }

        public ReadOnlySpan<T> Enabled => _enabledEntities.AsReadonlySpan();
        public ReadOnlySpan<T> Disabled => _disabledEntities.AsReadonlySpan();

        private ref ArrayBuilder<T> GetBucket(EntityBucket bucket)
        {
            return ref bucket == EntityBucket.Inactive
                ? ref _disabledEntities
                : ref _enabledEntities;
        }

        public EntityLocation Add(T entity, EntityBucket targetBucket)
        {
            ref ArrayBuilder<T> bucket = ref GetBucket(targetBucket);
            uint index = bucket.Count;
            bucket.Add(entity);
            return new EntityLocation(targetBucket, index);
        }

        public override EntityMove Remove(EntityLocation location)
        {
            ref ArrayBuilder<T> bucket = ref GetBucket(location.Bucket);
            if (location.Index == bucket.Count - 1)
            {
                bucket[location.Index] = null!;
                bucket.Truncate(bucket.Count - 1);
                return EntityMove.Empty;
            }
            var move = new EntityMove(bucket[^1], location);
            bucket.SwapRemove(location.Index);
            return move;
        }

        public override BucketChangeResult ChangeBucket(
            EntityLocation entityLocation,
            EntityBucket dstBucket)
        {
            EntityBucket srcBucket = entityLocation.Bucket;
            if (srcBucket != dstBucket)
            {
                ref ArrayBuilder<T> oldBucket = ref GetBucket(srcBucket);
                var swap = entityLocation.Index != oldBucket.Count - 1
                    ? new EntityMove(oldBucket[^1], entityLocation)
                    : EntityMove.Empty;
                T entity = oldBucket.SwapRemove(entityLocation.Index);
                EntityLocation newLocation = Add(entity, dstBucket);
                return new BucketChangeResult(newLocation, swap);
            }
            return new BucketChangeResult(entityLocation, EntityMove.Empty);
        }
    }

    internal sealed class SortableEntityGroup<T> : EntityGroup<T>
        where T : Entity, IComparable<T>
    {
        private T[] _sorted;

        public SortableEntityGroup(uint nbEnabled = 4, uint nbDisabled = 4)
            : base(nbEnabled, nbDisabled)
        {
            _sorted = new T[nbEnabled];
        }

        public ReadOnlySpan<T> SortActive()
        {
            int actualCount = (int)_enabledEntities.Count;
            if (_sorted.Length < actualCount)
            {
                int newSize = MathUtil.RoundUp(actualCount, _sorted.Length);
                Array.Resize(ref _sorted, newSize);
            }
            else if (_sorted.Length > actualCount)
            {
                Array.Fill(_sorted, default!, actualCount, _sorted.Length - actualCount);
            }

            Array.Copy(_enabledEntities.UnderlyingArray, _sorted, actualCount);
            return _sorted.AsSpan(0, actualCount);
        }
    }

    internal sealed partial class World
    {
        private readonly struct EntityRec
        {
            public readonly Entity Entity;
            public readonly EntityGroup Group;
            public readonly EntityLocation Location;

            public EntityRec(
                Entity entity,
                EntityGroup group,
                EntityLocation location)
            {
                Entity = entity;
                Group = group;
                Location = location;
            }

            public bool IsEnabled
                => Location.Bucket == EntityBucket.Active;

            public EntityRec WithBucket(EntityBucket newBucket)
            {
                var location = new EntityLocation(newBucket, Location.Index);
                return new EntityRec(Entity, Group, location);
            }

            public EntityRec WithLocation(EntityLocation newLocation)
                => new EntityRec(Entity, Group, newLocation);
        }

        private readonly Dictionary<EntityId, EntityRec> _entities;
        private readonly Dictionary<EntityPath, EntityId> _aliases;

        private readonly List<(EntityId, EntityBucket)> _pendingBucketChanges;
        private readonly Dictionary<AnimationKey, PropertyAnimation> _activeAnimations;
        private readonly List<(AnimationKey, PropertyAnimation)> _animationsToDeactivate;
        private readonly List<(AnimationKey, PropertyAnimation)> _queuedAnimations;

        private readonly SortableEntityGroup<RenderItem> _renderItems;

        public World()
        {
            _entities = new Dictionary<EntityId, EntityRec>(1024);
            _aliases = new Dictionary<EntityPath, EntityId>(128);
            _renderItems = new SortableEntityGroup<RenderItem>();
            _pendingBucketChanges = new List<(EntityId, EntityBucket)>();
            _activeAnimations = new Dictionary<AnimationKey, PropertyAnimation>();
            _animationsToDeactivate = new List<(AnimationKey, PropertyAnimation)>();
            _queuedAnimations = new List<(AnimationKey, PropertyAnimation)>();
        }

        public EntityGroupView<RenderItem> RenderItems => _renderItems;

        public void BeginFrame()
        {
            foreach ((AnimationKey key, PropertyAnimation anim) in _queuedAnimations)
            {
                _activeAnimations[key] = anim;
            }
            _queuedAnimations.Clear();

            foreach ((EntityId entityId, EntityBucket dstBucket) in _pendingBucketChanges)
            {
                ChangeBucket(entityId, dstBucket);
            }
            _pendingBucketChanges.Clear();
        }

        public ResolvedEntityPath ResolvePath(in EntityPath path)
        {
            return ResolvePath(path, out ResolvedEntityPath result)
                ? result
                : throw new ArgumentException($"Invalid entity path: '{path.Value}'.");
        }

        public bool ResolvePath(in EntityPath path, out ResolvedEntityPath resolvedPath)
        {
            EntityId id = Resolve(path);
            EntityId parentId = EntityId.Invalid;
            if (path.GetParent(out EntityPath parentPath))
            {
                parentId = Resolve(parentPath);
                if (Get(parentId) is null)
                {
                    resolvedPath = default;
                    return false;
                }
            }
            resolvedPath = new ResolvedEntityPath(id, parentId);
            return true;
        }

        public Entity? Get(in EntityPath entityPath)
            => Get(Resolve(entityPath));

        public Entity? Get(in EntityId entityId)
            => _entities.TryGetValue(entityId, out EntityRec rec) ? rec.Entity : null;

        public void AddRenderItem(RenderItem renderItem)
        {
            Add(renderItem, _renderItems);
        }

        public void SetAlias(in EntityId entityId, in EntityPath alias)
        {
            if (Get(alias) is object)
            {
                throw new ArgumentException($"'{alias}' resolves to an existing entity.");
            }

            if (Get(entityId) is Entity entity)
            {
                if (!entity.Alias.IsEmpty)
                {
                    _aliases.Remove(entity.Alias);
                }
                _aliases[alias] = entityId;
                ((EntityInternal)entity).SetAlias(alias);
            }
        }

        public bool IsEnabled(in EntityId entityId)
        {
            EntityRec rec = GetRecord(entityId);
            return rec.IsEnabled;
        }

        public bool IsEnabled(Entity entity)
            => IsEnabled(entity.Id);

        public void EnableEntity(in EntityId id)
            => SetEnabled(id, true);

        public void EnableEntity(Entity entity)
            => SetEnabled(entity.Id, true);

        public void DisableEntity(in EntityId id)
            => SetEnabled(id, false);

        public void DisableEntity(Entity entity)
            => SetEnabled(entity.Id, false);

        public void DestroyEntity(Entity entity)
           => DestroyEntity(entity.Id);

        public void DestroyEntity(in EntityId id)
        {
            EntityRec rec = GetRecord(id);
            Entity entity = rec.Entity;
            if (entity.HasParent)
            {
                Entity parent = GetRecord(entity.Parent).Entity;
                ((EntityInternal)parent).RemoveChild(id);
            }
            foreach (EntityId child in entity.Children)
            {
                DestroyEntity(child);
            }
            if (!entity.Alias.IsEmpty)
            {
                _aliases.Remove(entity.Alias);
            }

            EntityMove move = rec.Group.Remove(rec.Location);
            UpdateLocation(move);
            _entities.Remove(id);
            entity.Dispose();
        }

        private void Add<T>(T entity, EntityGroup<T> group)
          where T : Entity
        {
            EntityId id = entity.Id;
            if (entity.Parent.IsValid)
            {
                EntityRec parentRec = GetRecord(entity.Parent);
                ((EntityInternal)parentRec.Entity).AddChild(id);
            }

            EntityLocation location = group.Add(entity, EntityBucket.Inactive);
            _entities[id] = new EntityRec(
                entity,
                group,
                location
            );

            _pendingBucketChanges.Add((id, EntityBucket.Active));
        }

        private EntityId Resolve(in EntityPath path)
        {
            EntityId lookupAlias(in EntityPath alias)
            {
                return _aliases.TryGetValue(alias, out EntityId result)
                    ? result : EntityId.Invalid;
            }

            return path.Value[0] != '@'
                ? new EntityId(path.Value, path.NameStartIndex, path.MouseState)
                : lookupAlias(path);
        }

        private EntityRec GetRecord(in EntityId entityId)
        {
            return _entities.TryGetValue(entityId, out EntityRec rec)
                ? rec
                : throw new ArgumentException($"Entity '{entityId.ToString()}' does not exist.");
        }

        private void SetEnabled(in EntityId entityId, bool enable)
        {
            EntityBucket dstBucket = enable
                ? EntityBucket.Active
                : EntityBucket.Inactive;
            ChangeBucket(entityId, dstBucket);
        }

        private void ChangeBucket(in EntityId entityId, EntityBucket dstBucket)
        {
            EntityRec rec = GetRecord(entityId);
            BucketChangeResult result = rec.Group.ChangeBucket(rec.Location, dstBucket);
            _entities[entityId] = rec.WithBucket(dstBucket);
            SetLocation(entityId, result.NewLocation);
            UpdateLocation(result.Swap);
        }

        private void UpdateLocation(EntityMove entityMove)
        {
            if (entityMove.Entity != null)
            {
                EntityId id = entityMove.Entity.Id;
                EntityRec rec = _entities[id];
                _entities[id] = rec.WithLocation(entityMove.NewLocation);
            }
        }

        private void SetLocation(in EntityId entityId, EntityLocation location)
        {
            EntityRec rec = _entities[entityId];
            _entities[entityId] = rec.WithLocation(location);
        }

        public bool TryGetAnimation<T>(Entity entity, [NotNullWhen(true)] out T? animation)
            where T : PropertyAnimation
        {
            var key = new AnimationKey(entity, typeof(T));
            _activeAnimations.TryGetValue(key, out PropertyAnimation? val);
            return (animation = val as T) is object;
        }

        public void ActivateAnimation<T>(T animation)
            where T : PropertyAnimation
        {
            var key = new AnimationKey(animation.Entity, typeof(T));
            _queuedAnimations.Add((key, animation));
        }

        public void DeactivateAnimation(PropertyAnimation animation)
        {
            var key = new AnimationKey(animation.Entity, animation.GetType());
            _animationsToDeactivate.Add((key, animation));
        }

        public void FlushDetachedAnimations()
        {
            foreach ((AnimationKey key, PropertyAnimation anim) in _animationsToDeactivate)
            {
                if (_activeAnimations.TryGetValue(key, out PropertyAnimation? value) && value == anim)
                {
                    _activeAnimations.Remove(key);
                }
            }
            _animationsToDeactivate.Clear();
        }

        internal readonly struct AnimationKey : IEquatable<AnimationKey>
        {
            public readonly Entity Entity;
            public readonly Type RuntimeType;

            public AnimationKey(Entity entity, Type runtimeType)
            {
                Entity = entity;
                RuntimeType = runtimeType;
            }

            public bool Equals(AnimationKey other)
                => Entity.Equals(other.Entity) && RuntimeType == other.RuntimeType;

            public override int GetHashCode()
                => HashCode.Combine(Entity, RuntimeType);
        }
    }
}
