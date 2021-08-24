using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.NsScript;
using NitroSharp.Saving;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal sealed partial class World : IDisposable
    {
        internal readonly struct EntityRec
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
                => new(Entity, Group, newLocation);
        }

        private sealed class EntityContext
        {
            private ArrayBuilder<EntityId> _entities;

            public EntityContext()
            {
                _entities = new ArrayBuilder<EntityId>(16);
            }

            public ReadOnlySpan<EntityId> Entities => _entities.AsReadonlySpan();
            public void Add(in EntityId entity) => _entities.Add(entity);
        }

        private readonly Dictionary<uint, EntityContext> _contextLookup;
        private readonly Dictionary<EntityPath, EntityId> _aliases;

        private readonly Dictionary<EntityId, EntityRec> _entities;
        private readonly List<(EntityId, EntityBucket)> _pendingBucketChanges;
        private readonly Queue<EntityId> _markedEntities;
        private readonly Queue<EntityId> _survivedEntities;

        private readonly EntityGroup<BasicEntity> _basicEntities;
        private readonly EntityGroup<VmThread> _vmThreads;
        private readonly SortableEntityGroup<RenderItem> _renderItems;
        private readonly EntityGroup<ColorSource> _colorSources;
        private readonly EntityGroup<Image> _images;
        private readonly EntityGroup<Choice> _choices;
        private readonly EntityGroup<Sound> _sounds = new();

        public World()
        {
            _contextLookup = new Dictionary<uint, EntityContext>();
            _aliases = new Dictionary<EntityPath, EntityId>();

            _entities = new Dictionary<EntityId, EntityRec>(512);
            _pendingBucketChanges = new List<(EntityId, EntityBucket)>();
            _markedEntities = new Queue<EntityId>();
            _survivedEntities = new Queue<EntityId>();

            _basicEntities = new EntityGroup<BasicEntity>();
            _vmThreads = new EntityGroup<VmThread>();
            _renderItems = new SortableEntityGroup<RenderItem>();
            _colorSources = new EntityGroup<ColorSource>();
            _images = new EntityGroup<Image>();
            _choices = new EntityGroup<Choice>();
        }

        public SortableEntityGroupView<RenderItem> RenderItems => _renderItems;
        public EntityGroupView<Choice> Choices => _choices;
        public EntityGroup<Sound> Sounds => _sounds;

        public void DestroyContext(uint id)
        {
            if (_contextLookup.TryGetValue(id, out EntityContext? ctx))
            {
                _contextLookup.Remove(id);
                foreach (EntityId entityId in ctx.Entities)
                {
                    if (Get(entityId) is Entity { IsIdle: true, IsLocked: false } entity)
                    {
                        DestroyEntity(entity);
                    }
                }
            }
        }

        public void BeginFrame()
        {
            foreach ((EntityId entityId, EntityBucket dstBucket) in _pendingBucketChanges)
            {
                ChangeBucket(entityId, dstBucket);
            }
            _pendingBucketChanges.Clear();

            while (_markedEntities.TryDequeue(out EntityId id))
            {
                if (Get(id) is Entity entity)
                {
                    if (entity.IsIdle)
                    {
                        DestroyEntity(id);
                    }
                    else
                    {
                        _survivedEntities.Enqueue(id);
                    }
                }
            }

            while (_survivedEntities.TryDequeue(out EntityId survived))
            {
                _markedEntities.Enqueue(survived);
            }
        }

        public Entity? Get(in EntityId entityId)
            => _entities.TryGetValue(entityId, out EntityRec rec) ? rec.Entity : null;

        public bool Exists(in EntityId entityId)
            => Get(entityId) is not null;

        public void SetAlias(in EntityId entityId, in EntityPath alias)
        {
            if (Get(entityId) is Entity entity)
            {
                if (!entity.Alias.IsEmpty)
                {
                    _aliases.Remove(entity.Alias);
                }

                EntityPath aliasActual = !alias.Value.StartsWith('@')
                    ? new EntityPath("@" + alias.Value)
                    : alias;
                _aliases[aliasActual] = entityId;
                ((EntityInternal)entity).SetAlias(aliasActual);
            }
        }

        public Entity? Get(uint contextId, in EntityPath entityPath)
        {
            return CreateId(contextId, entityPath) is EntityId { IsValid: true } id
                ? Get(id)
                : null;
        }

        public ResolvedEntityPath ResolvePath(uint contextId, in EntityPath path)
        {
            return ResolvePath(contextId, path, out ResolvedEntityPath result)
                ? result
                : throw new ArgumentException($"Invalid entity path: '{path.Value}'.");
        }

        public bool ResolvePath(uint contextId, in EntityPath path, out ResolvedEntityPath resolvedPath)
        {
            resolvedPath = default;
            if (CreateId(contextId, path) is not EntityId { IsValid: true } id)
            {
                return false;
            }

            if (path.GetParent(out EntityPath parentPath)
                && CreateId(contextId, parentPath) is { IsValid: true } parentId)
            {
                if (Get(parentId) is Entity parent)
                {
                    resolvedPath = new ResolvedEntityPath(parentId.Context, id, parent);
                    return true;
                }

                resolvedPath = default;
                return false;
            }

            resolvedPath = new ResolvedEntityPath(contextId, id, parent: null);
            return true;
        }

        public BasicEntity Add(BasicEntity entity, bool enable = true)
        {
            Add(entity, _basicEntities, enable);
            return entity;
        }

        public void Add(VmThread thread, bool enable = true)
            => Add(thread, _vmThreads, enable);

        public T Add<T>(T renderItem, bool enable = true)
            where T : RenderItem
        {
            Add(renderItem, _renderItems, enable);
            return renderItem;
        }

        public void Add(ColorSource colorSource, bool enable = true)
            => Add(colorSource, _colorSources, enable);

        public void Add(Image image, bool enable = true)
            => Add(image, _images, enable);

        public void Add(Choice choice, bool enable = true)
            => Add(choice, _choices, enable);

        public Sound Add(Sound sound, bool enable = true)
        {
            Add(sound, _sounds, enable);
            return sound;
        }

        public bool IsEnabled(Entity entity)
        {
            EntityRec rec = _entities[entity.Id];
            return rec.Location.Bucket == EntityBucket.Active;
        }

        public void EnableEntity(Entity entity)
            => _pendingBucketChanges.Add((entity.Id, EntityBucket.Active));

        public void DisableEntity(Entity entity)
            => _pendingBucketChanges.Add((entity.Id, EntityBucket.Inactive));

        public void DestroyEntity(Entity entity)
        {
            DestroyEntity(entity.Id);
        }

        public void DestroyEntity(in EntityId id)
        {
            if (!GetRecord(id, out EntityRec rec)) { return; }
            Entity entity = rec.Entity;
            EntityMove move = rec.Group.Remove(rec.Location);
            UpdateLocation(move);

            if (entity.Parent is Entity parent)
            {
                ((EntityInternal)parent).RemoveChild(entity);
            }

            ref ArrayBuilder<Entity> children = ref ((EntityInternal)entity).GetChildrenMut();
            foreach (Entity child in children.AsSpan().ToArray())
            {
                DestroyEntity(child);
            }
            children.Clear();

            if (!entity.Alias.IsEmpty)
            {
                _aliases.Remove(entity.Alias);
            }

            _entities.Remove(id);
            entity.Dispose();
        }

        public void DestroyWhenIdle(Entity entity)
        {
            _markedEntities.Enqueue(entity.Id);
        }

        private bool GetRecord(in EntityId entiytId, out EntityRec rec)
            => _entities.TryGetValue(entiytId, out rec);

        private EntityId CreateId(uint contextId, in EntityPath path)
        {
            EntityId lookupSlow(in EntityPath path)
            {
                if (!path.GetParent(out EntityPath parentAlias))
                {
                    return _aliases.TryGetValue(path, out EntityId id)
                        ? id
                        : EntityId.Invalid;
                }

                if (CreateId(contextId, parentAlias) is EntityId { IsValid: true } parentId)
                {
                    string newPath = path.Value.Replace(parentAlias.Value, parentId.Path);
                    return new EntityId(
                        parentId.Context,
                        newPath,
                        parentId.Path.Length + 1,
                        path.MouseState
                    );
                }

                return EntityId.Invalid;
            }

            return path.Value[0] != '@'
                ? new EntityId(contextId, path.Value, path.NameStartIndex, path.MouseState)
                : lookupSlow(path);
        }

        private void Add<T>(T entity, EntityGroup<T> group, bool enable)
            where T : Entity
        {
            EntityId id = entity.Id;
            if (Get(id) is object)
            {
                DestroyEntity(id);
            }

            if (entity.Parent is Entity parent)
            {
                ((EntityInternal)parent).AddChild(entity);
            }

            EntityLocation location = group.Add(entity, EntityBucket.Inactive);
            _entities[id] = new EntityRec(
                entity,
                group,
                location
            );

            if (enable)
            {
                _pendingBucketChanges.Add((id, EntityBucket.Active));
            }

            if (!_contextLookup.TryGetValue(id.Context, out EntityContext? context))
            {
                context = new EntityContext();
                _contextLookup.Add(id.Context, context);
            }
            context.Add(id);
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

        private readonly struct EntitySaveDataLocation
        {
            public EntitySaveDataLocation(EntityKind entityKind, int index, EntityId parentId)
            {
                EntityKind = entityKind;
                Index = index;
                ParentId = parentId;
            }

            public EntityKind EntityKind { get; }
            public int Index { get; }
            public EntityId ParentId { get; }
        }

        public WorldSaveData ToSaveData(GameSavingContext ctx) => new()
        {
            Aliases = _aliases.Select(x => (x.Key.Value, x.Value)).ToArray(),
            BasicEntities = _basicEntities.CollectAll().Select(x => x.ToSaveData(ctx)).ToArray(),
            VmThreads = _vmThreads.CollectAll().Select(x => x.ToSaveData(ctx)).ToArray(),
            Images = _images.CollectAll().Select(x => x.ToSaveData(ctx)).ToArray(),
            ColorSources = _colorSources.CollectAll().Select(x => x.ToSaveData(ctx)).ToArray(),
            AlphaMasks = _renderItems.CollectAll().OfType<AlphaMask>().Select(x => x.ToSaveData(ctx)).ToArray(),
            DialogueBoxes = _renderItems.CollectAll().OfType<DialogueBox>().Select(x => x.ToSaveData(ctx)).ToArray(),
            DialoguePages = _renderItems.CollectAll().OfType<DialoguePage>().Select(x => x.ToSaveData(ctx)).ToArray(),
            TextBlocks = _renderItems.CollectAll().OfType<TextBlock>().Select(x => x.ToSaveData(ctx)).ToArray(),
            Sprites = _renderItems.CollectAll().OfType<Sprite>().Select(x => x.ToSaveData(ctx)).ToArray(),
            Cubes = _renderItems.CollectAll().OfType<Cube>().Select(x => x.ToSaveData(ctx)).ToArray(),
            Choices = _choices.CollectAll().Select(x => x.ToSaveData(ctx)).ToArray()
        };

        public static World Load(WorldSaveData saveData, GameLoadingContext loadCtx)
        {
            var world = new World();
            var index = new Dictionary<EntityId, EntitySaveDataLocation>();

            void indexTable(EntityKind entityKind, IEntitySaveData[] table)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    EntitySaveData entityData = table[i].CommonEntityData;
                    index[entityData.Id] = new EntitySaveDataLocation(entityKind, i, entityData.Parent);
                }
            }

            (EntityKind, IEntitySaveData[])[] entityTables = saveData
                .EnumerateEntityTables()
                .ToArray();

            foreach ((EntityKind kind, IEntitySaveData[] table) in entityTables)
            {
                indexTable(kind, table);
            }

            foreach ((EntityKind _, IEntitySaveData[] table) in entityTables)
            {
                foreach (EntityId entityId in table.Select(x => x.CommonEntityData.Id))
                {
                    createEntity(ref saveData, entityId);
                }
            }

            foreach ((string alias, EntityId id) in saveData.Aliases)
            {
                world.SetAlias(id, new EntityPath(alias));
            }

            void createEntity(ref WorldSaveData worldData, EntityId id)
            {
                if (world.Get(id) is not null)
                {
                    return;
                }

                EntitySaveDataLocation saveDataLoc = index[id];
                Entity? parent = null;
                if (saveDataLoc.ParentId.IsValid)
                {
                    if (world.Get(saveDataLoc.ParentId) is null)
                    {
                        createEntity(ref worldData, saveDataLoc.ParentId);
                    }

                    parent = world.Get(saveDataLoc.ParentId);
                }

                var resolvedPath = new ResolvedEntityPath(id.Context, id, parent);
                switch (saveDataLoc.EntityKind)
                {
                    case EntityKind.Basic:
                        BasicEntitySaveData entityData = worldData.BasicEntities[saveDataLoc.Index];
                        var entity = new BasicEntity(resolvedPath, entityData);
                        world.Add(entity, entityData.Data.IsEnabled);
                        break;
                    case EntityKind.VmThread:
                        VmThreadSaveData threadData = worldData.VmThreads[saveDataLoc.Index];
                        var thread = new VmThread(resolvedPath, threadData, loadCtx.VM, loadCtx.Process.VmProcess);
                        world.Add(thread, threadData.Common.IsEnabled);
                        break;
                    case EntityKind.Image:
                        ImageSaveData imageData = worldData.Images[saveDataLoc.Index];
                        var image = new Image(resolvedPath, imageData, loadCtx);
                        world.Add(image, imageData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.ColorSource:
                        ColorSourceSaveData colorSourceData = worldData.ColorSources[saveDataLoc.Index];
                        var colorSource = new ColorSource(resolvedPath, colorSourceData);
                        world.Add(colorSource, colorSourceData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.AlphaMask:
                        ConstraintBoxSaveData alphaMaskData = worldData.AlphaMasks[saveDataLoc.Index];
                        var mask = new AlphaMask(resolvedPath, alphaMaskData);
                        world.Add(mask, alphaMaskData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.DialogueBox:
                        ConstraintBoxSaveData boxData = worldData.DialogueBoxes[saveDataLoc.Index];
                        var box = new DialogueBox(resolvedPath, boxData);
                        world.Add(box, boxData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.DialoguePage:
                        DialoguePageSaveData pageData = worldData.DialoguePages[saveDataLoc.Index];
                        var page = new DialoguePage(resolvedPath, pageData, loadCtx);
                        world.Add(page, pageData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.TextBlock:
                        TextBlockSaveData textBlockData = worldData.TextBlocks[saveDataLoc.Index];
                        var textBlock = new TextBlock(resolvedPath, textBlockData, loadCtx);
                        world.Add(textBlock, textBlockData.Common.EntityData.IsEnabled);
                        break;
                    case EntityKind.Sprite:
                        SpriteSaveData spriteData = worldData.Sprites[saveDataLoc.Index];
                        var sprite = new Sprite(resolvedPath, spriteData, loadCtx);
                        world.Add(sprite, spriteData.CommonEntityData.IsEnabled);
                        break;
                    case EntityKind.Cube:
                        CubeSaveData cubeData = worldData.Cubes[saveDataLoc.Index];
                        var cube = new Cube(resolvedPath, cubeData, loadCtx.Rendering);
                        world.Add(cube, cubeData.Common.EntityData.IsEnabled);
                        break;
                    case EntityKind.Choice:
                        ChoiceSaveData choiceData = worldData.Choices[saveDataLoc.Index];
                        var choice = new Choice(resolvedPath, choiceData, world);
                        world.Add(choice, choiceData.Common.IsEnabled);
                        break;
                }
            }

            return world;
        }

        public void Reset()
        {
            foreach ((_, EntityRec rec) in _entities)
            {
                if (!(rec.Entity.IsLocked && rec.Entity.Id.Context == 1))
                {
                    DestroyEntity(rec.Entity);
                }
                else if (rec.Entity is RenderItem renderItem)
                {
                    renderItem.Fade(0, TimeSpan.Zero);
                }
            }
        }

        public void Dispose()
        {
            foreach ((_, EntityRec rec) in _entities)
            {
                rec.Entity.Dispose();
            }
        }
    }

    internal readonly struct ResolvedEntityPath
    {
        public readonly uint ContextId;
        public readonly EntityId Id;
        public readonly Entity? Parent;

        public ResolvedEntityPath(
            uint contextId,
            in EntityId id,
            Entity? parent)
            => (ContextId, Id, Parent) = (contextId, id, parent);
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

        private EntityGroupView(EntityGroup<T> group)
        {
            _group = group;
        }

        public ReadOnlySpan<T> Enabled => _group.Enabled;
        public ReadOnlySpan<T> Disabled => _group.Disabled;

        public static implicit operator EntityGroupView<T>(EntityGroup<T> group)
            => new(group);
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
            => new(group);
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

        public T[] CollectAll()
        {
            var array = new T[Enabled.Length + Disabled.Length];
            Enabled.CopyTo(array.AsSpan()[..Enabled.Length]);
            Disabled.CopyTo(array.AsSpan()[Enabled.Length..]);
            return array;
        }

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
                bucket[location.Index] = default!;
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

    [Persistable]
    internal readonly partial struct WorldSaveData
    {
        public (string, EntityId)[] Aliases { get; init; }
        public BasicEntitySaveData[] BasicEntities { get; init; }
        public VmThreadSaveData[] VmThreads { get; init; }
        public ImageSaveData[] Images { get; init; }
        public ColorSourceSaveData[] ColorSources { get; init; }
        public ConstraintBoxSaveData[] AlphaMasks { get; init; }
        public ConstraintBoxSaveData[] DialogueBoxes { get; init; }
        public DialoguePageSaveData[] DialoguePages { get; init; }
        public TextBlockSaveData[] TextBlocks { get; init; }
        public SpriteSaveData[] Sprites { get; init; }
        public CubeSaveData[] Cubes { get; init; }
        public ChoiceSaveData[] Choices { get; init; }

        public IEnumerable<(EntityKind, IEntitySaveData[])> EnumerateEntityTables()
        {
            yield return (EntityKind.Basic, BasicEntities.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.VmThread, VmThreads.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.Image, Images.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.ColorSource, ColorSources.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.AlphaMask, AlphaMasks.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.DialogueBox, DialogueBoxes.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.DialoguePage, DialoguePages.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.TextBlock, TextBlocks.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.Sprite, Sprites.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.Cube, Cubes.Cast<IEntitySaveData>().ToArray());
            yield return (EntityKind.Choice, Choices.Cast<IEntitySaveData>().ToArray());
        }
    }
}
