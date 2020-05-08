using System;
using System.Collections.Generic;
using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp
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

    internal readonly struct SortableEntityGroupView<T>
        where T : Entity, IComparable<T>
    {
        private readonly SortableEntityGroup<T> _group;

        public SortableEntityGroupView(SortableEntityGroup<T> group)
        {
            _group = group;
        }

        public ReadOnlySpan<T> Enabled => _group.Enabled;
        public ReadOnlySpan<T> Disabled => _group.Disabled;

        public ReadOnlySpan<T> SortActive() => _group.SortActive();

        public static implicit operator SortableEntityGroupView<T>(
            SortableEntityGroup<T> group)
            => new SortableEntityGroupView<T>(group);
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
            Array.Sort(_sorted, 0, actualCount);
            return _sorted.AsSpan(0, actualCount);
        }
    }

    internal sealed partial class World : IDisposable
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

        private readonly EntityGroup<VmThread> _vmThreads;
        private readonly SortableEntityGroup<RenderItem> _renderItems;
        private readonly EntityGroup<Image> _images;
        private readonly EntityGroup<Choice> _choices;

        public World()
        {
            _entities = new Dictionary<EntityId, EntityRec>(1024);
            _aliases = new Dictionary<EntityPath, EntityId>(128);
            _vmThreads = new EntityGroup<VmThread>();
            _renderItems = new SortableEntityGroup<RenderItem>();
            _images = new EntityGroup<Image>();
            _choices = new EntityGroup<Choice>();
            _pendingBucketChanges = new List<(EntityId, EntityBucket)>();
        }

        public EntityGroupView<VmThread> Threads => _vmThreads;
        public SortableEntityGroupView<RenderItem> RenderItems => _renderItems;
        public EntityGroupView<Image> Images => _images;
        public EntityGroupView<Choice> Choices => _choices;

        public void BeginFrame()
        {
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

        public VmThread Add(VmThread thread)
        {
            Add(thread, _vmThreads);
            return thread;
        }

        public T Add<T>(T renderItem)
            where T : RenderItem
        {
            Add(renderItem, _renderItems);
            return renderItem;
        }

        public Image Add(Image image)
        {
            Add(image, _images);
            return image;
        }

        public Choice Add(Choice choice)
        {
            Add(choice, _choices);
            return choice;
        }

        public void SetAlias(in EntityId entityId, in EntityPath alias)
        {
            if (Get(alias) is Entity existing)
            {
                if (ReferenceEquals(Get(entityId), existing)) { return; }
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

        public ChildEnumerable<T> Children<T>(Entity entity)
            where T : Entity
        {
            return new ChildEnumerable<T>(this, entity.Children);
        }

        public bool IsEnabled(in EntityId entityId)
        {
            EntityRec rec = GetRecord(entityId);
            return rec.IsEnabled;
        }

        public bool IsEnabled(Entity entity)
            => IsEnabled(entity.Id);

        public void EnableEntity(Entity entity)
            => SetEnabled(entity.Id, true);

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

            EntityMove move = rec.Group.Remove(rec.Location);
            UpdateLocation(move);
            foreach (EntityId child in entity.Children)
            {
                DestroyEntity(child);
            }
            if (!entity.Alias.IsEmpty)
            {
                _aliases.Remove(entity.Alias);
            }

            _entities.Remove(id);
            entity.Dispose();
        }

        private void Add<T>(T entity, EntityGroup<T> group)
          where T : Entity
        {
            EntityId id = entity.Id;
            if (Get(id) is object)
            {
                DestroyEntity(id);
            }

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
            if (_entities.TryGetValue(entityId, out EntityRec rec))
            {
                BucketChangeResult result = rec.Group.ChangeBucket(rec.Location, dstBucket);
                _entities[entityId] = rec.WithBucket(dstBucket);
                SetLocation(entityId, result.NewLocation);
                UpdateLocation(result.Swap);
            }
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

        public void Dispose()
        {
            foreach ((_, EntityRec rec) in _entities)
            {
                rec.Entity.Dispose();
            }
        }

        internal readonly ref struct ChildEnumerable<T>
            where T : Entity
        {
            private readonly World _world;
            private readonly ReadOnlySpan<EntityId> _ids;

            public ChildEnumerable(World world, ReadOnlySpan<EntityId> ids)
            {
                _world = world;
                _ids = ids;
            }

            public ChildEnumerator<T> GetEnumerator()
                => new ChildEnumerator<T>(_world, _ids);
        }

        internal ref struct ChildEnumerator<T>
            where T : Entity
        {
            private readonly World _world;
            private readonly ReadOnlySpan<EntityId> _ids;
            private int _pos;
            private T? _current;

            public ChildEnumerator(World world, ReadOnlySpan<EntityId> ids)
            {
                _world = world;
                _ids = ids;
                _pos = 0;
                _current = null;
            }

            public T Current => _current!;

            public bool MoveNext()
            {
                _current = null;
                while (_pos < _ids.Length && _current is null)
                {
                    _current = _world.Get(_ids[_pos++]) as T;
                }
                return _current is object;
            }
        }
    }
}
