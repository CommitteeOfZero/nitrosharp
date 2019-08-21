using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using NitroSharp.Animation;
using NitroSharp.Graphics;
using NitroSharp.Media;
using NitroSharp.Content;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal sealed class World
    {
        public const ushort InitialCapacity = 1024;
        public const ushort InitialSpriteCount = 768;
        public const ushort InitialRectangleCount = 32;
        public const ushort InitialTextLayoutCount = 32;
        public const ushort InitialAudioClipCount = 64;
        public const ushort InitialVideoClipCount = 4;

        private readonly Dictionary<string, Entity> _entities;
        private readonly Dictionary<string, string> _aliases;
        private readonly List<EntityTable> _tables;
        private ArrayBuilder<EntityEvent> _entityEvents;
        private ushort _nextEntityId = 1;

        private readonly Dictionary<AnimationDictionaryKey, PropertyAnimation> _activeAnimations;
        private readonly List<(AnimationDictionaryKey key, PropertyAnimation anim)> _animationsToDeactivate;
        private readonly List<AnimationEvent> _animationEvents;

        public World(bool isPrimary)
        {
            IsPrimary = isPrimary;
            _entities = new Dictionary<string, Entity>(InitialCapacity);
            _aliases = new Dictionary<string, string>();
            _entityEvents = new ArrayBuilder<EntityEvent>(initialCapacity: 128);
            _tables = new List<EntityTable>(8);

            Threads = RegisterTable(new ThreadTable(this, 32));
            Sprites = RegisterTable(new SpriteTable(this, InitialSpriteCount));
            Rectangles = RegisterTable(new RectangleTable(this, InitialRectangleCount));
            TextInstances = RegisterTable(new TextInstanceTable(this, InitialTextLayoutCount));
            AudioClips = RegisterTable(new AudioClipTable(this, InitialAudioClipCount));
            VideoClips = RegisterTable(new VideoClipTable(this, InitialVideoClipCount));
            Choices = RegisterTable(new ChoiceTable(this, 32));

            _activeAnimations = new Dictionary<AnimationDictionaryKey, PropertyAnimation>();
            _animationsToDeactivate = new List<(AnimationDictionaryKey key, PropertyAnimation anim)>();
            _animationEvents = new List<AnimationEvent>();
        }

        public bool IsPrimary { get; }

        public ThreadTable Threads { get; }
        public SpriteTable Sprites { get; }
        public RectangleTable Rectangles { get; }
        public TextInstanceTable TextInstances { get; }
        public AudioClipTable AudioClips { get; }
        public VideoClipTable VideoClips { get; }
        public ChoiceTable Choices { get; }

        public Dictionary<string, Entity>.Enumerator EntityEnumerator => _entities.GetEnumerator();
        public Dictionary<AnimationDictionaryKey, PropertyAnimation>.ValueCollection AttachedAnimations
            => _activeAnimations.Values;

        private T RegisterTable<T>(T table) where T : EntityTable
        {
            _tables.Add(table);
            return table;
        }

        public T GetTable<T>(Entity entity) where T : EntityTable
            => (T)_tables[(int)entity.Kind];

        public T GetEntityStruct<T>(Entity entity) where T : EntityStruct
        {
            EntityTable table = GetTable<EntityTable>(entity);
            return table.Get<T>(entity);
        }

        public T GetMutEntityStruct<T>(Entity entity) where T : MutableEntityStruct
        {
            EntityTable table = GetTable<EntityTable>(entity);
            return table.GetMutable<T>(entity);
        }

        public bool TryGetEntity(string name, out Entity entity)
            => _entities.TryGetValue(name, out entity);

        public bool IsEntityAlive(Entity entity)
        {
            var table = GetTable<EntityTable>(entity);
            return table.EntityExists(entity);
        }

        public void SetAlias(string name, string alias)
        {
            if (TryGetEntity(name, out Entity entity) || TryGetEntity(alias, out entity))
            {
                _entities[name] = entity;
                _entities[alias] = entity;
                _aliases[name] = alias;
                _aliases[alias] = name;
                ref EntityEvent evt = ref _entityEvents.Add();
                evt.EventKind = EntityEventKind.AliasAdded;
                evt.Entity = entity;
                evt.EntityName = name;
                evt.Alias = alias;
            }
        }

        public Entity CreateThreadEntity(in InterpreterThreadInfo threadInfo)
        {
            Entity entity = CreateEntity(threadInfo.Name, EntityKind.Thread);
            Threads.Infos.Set(entity, threadInfo);
            return entity;
        }

        public Entity CreateSprite(
            string name, AssetId image, in RectangleF sourceRectangle,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.Sprite, renderPriority, size, ref color);
            Sprites.ImageSources.Set(entity, new ImageSource(image, sourceRectangle));
            return entity;
        }

        public Entity CreateRectangle(string name, int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.Rectangle, renderPriority, size, ref color);
            return entity;
        }

        public Entity CreateTextInstance(string name, TextLayout layout, int renderPriority, ref RgbaFloat color)
        {
            var bounds = new SizeF(layout.MaxBounds.Width, layout.MaxBounds.Height);
            Entity entity = CreateVisual(name, EntityKind.Text, renderPriority, bounds, ref color);
            TextInstances.Layouts.Set(entity, ref layout);
            TextInstances.ClearFlags.Set(entity, true);
            return entity;
        }

        public Entity CreateAudioClip(string name, AssetId asset, bool enableLooping)
        {
            Entity entity = CreateEntity(name, EntityKind.AudioClip);
            AudioClips.Asset.Set(entity, asset);
            AudioClips.LoopData.Set(entity, new MediaClipLoopData(enableLooping, null));
            AudioClips.Volume.Set(entity, 1.0f);
            return entity;
        }

        public Entity CreateVideoClip(string name, AssetId asset, bool enableLooping, int renderPriority, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.VideoClip, renderPriority, default, ref color);
            VideoClips.Asset.Set(entity, asset);
            VideoClips.LoopData.Set(entity, new MediaClipLoopData(enableLooping, null));
            VideoClips.Volume.Set(entity, 1.0f);
            return entity;
        }

        public Entity CreateChoice(string name)
        {
            Entity entity = CreateEntity(name, EntityKind.Choice);
            Choices.Name.Set(entity, name);
            return entity;
        }

        private Entity CreateVisual(
            string name, EntityKind kind,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateEntity(name, kind);
            RenderItemTable table = GetTable<RenderItemTable>(entity);
            table.SortKeys.Set(entity, new RenderItemKey((ushort)renderPriority, entity.Id));
            table.Bounds.Set(entity, size);
            table.Colors.Set(entity, ref color);
            table.TransformComponents.Mutate(entity).Scale = Vector3.One;
            return entity;
        }

        public void ActivateAnimation<T>(T animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(animation.Entity, typeof(T));
            _activeAnimations[key] = animation;
            _animationEvents.Add(new AnimationEvent(key, AnimationEventKind.AnimationActivated));
        }

        public void DeactivateAnimation(PropertyAnimation animation)
        {
            var key = new AnimationDictionaryKey(animation.Entity, animation.GetType());
            _animationsToDeactivate.Add((key, animation));
            _animationEvents.Add(new AnimationEvent(key, AnimationEventKind.AnimationDeactivated));
        }

        public bool TryGetAnimation<T>(Entity entity, out T? animation) where T : PropertyAnimation
        {
            var key = new AnimationDictionaryKey(entity, typeof(T));
            bool result = _activeAnimations.TryGetValue(key, out PropertyAnimation? val);
            animation = val as T;
            return result;
        }

        public void FlushDetachedAnimations()
        {
            foreach ((var dictKey, var anim) in _animationsToDeactivate)
            {
                if (_activeAnimations.TryGetValue(dictKey, out var value) && value == anim)
                {
                    _activeAnimations.Remove(dictKey);
                }
            }
            _animationsToDeactivate.Clear();
        }

        public void FlushFrameEvents()
        {
            foreach (EntityTable table in _tables)
            {
                table.FlushFrameEvents();
            }
        }

        private Entity CreateEntity(string name, EntityKind kind)
        {
            if (_entities.TryGetValue(name, out _))
            {
                RemoveEntity(name);
            }

            EntityTable table = _tables[(int)kind];
            var handle = new Entity(_nextEntityId++, kind);
            table.Insert(handle);
            _entities[name] = handle;
            ref EntityEvent evt = ref _entityEvents.Add();
            evt.Entity = handle;
            evt.EntityName = name;
            evt.EventKind = EntityEventKind.EntityAdded;
            return handle;
        }

        public void RemoveEntity(string name)
        {
            Entity entity = RemoveEntityCore(name);
            if (entity.IsValid)
            {
                ref EntityEvent evt = ref _entityEvents.Add();
                evt.EntityName = name;
                evt.Entity = entity;
                evt.EventKind = EntityEventKind.EntityRemoved;
            }
        }

        private Entity RemoveEntityCore(string name)
        {
            if (_entities.TryGetValue(name, out Entity entity))
            {
                _entities.Remove(name);
                if (_aliases.TryGetValue(name, out string? alias))
                {
                    _entities.Remove(alias);
                    _aliases.Remove(name);
                    _aliases.Remove(alias);
                }

                var table = GetTable<EntityTable>(entity);
                table.Remove(entity);
                return entity;
            }

            return Entity.Invalid;
        }

        public void MergeChangesInto(World target)
        {
            if (_entityEvents.Count > 0 && target._entityEvents.Count > 0)
            {
                ThrowCannotMerge();
            }

            for (int i = 0; i < _entityEvents.Count; i++)
            {
                ref EntityEvent evt = ref _entityEvents[i];
                var table = target.GetTable<EntityTable>(evt.Entity);
                switch (evt.EventKind)
                {
                    case EntityEventKind.EntityAdded:
                        table.Insert(evt.Entity);
                        target._entities[evt.EntityName] = evt.Entity;
                        target._nextEntityId++;
                        break;
                    case EntityEventKind.EntityRemoved:
                        target.RemoveEntityCore(evt.EntityName);
                        break;
                    case EntityEventKind.AliasAdded:
                        target._entities[evt.Alias] = evt.Entity;
                        target._entities[evt.EntityName] = evt.Entity;
                        target._aliases[evt.EntityName] = evt.Alias;
                        target._aliases[evt.Alias] = evt.EntityName;
                        break;
                }
            }

            for (int i = 0; i < _tables.Count; i++)
            {
                _tables[i].MergeChanges(target._tables[i]);
                EntityTable.Debug_CompareTables(_tables[i], target._tables[i]);
            }

            foreach (AnimationEvent ae in _animationEvents)
            {
                if (ae.EventKind == AnimationEventKind.AnimationActivated)
                {
                    if (_activeAnimations.TryGetValue(ae.Key, out PropertyAnimation? animation))
                    {
                        target._activeAnimations[ae.Key] = animation;
                    }
                }
                else
                {
                    target._activeAnimations.Remove(ae.Key);
                }
            }

            Debug_EnsureMergedCorrectly(this, target);

            _animationEvents.Clear();
            _entityEvents.Reset();
        }

        [Conditional("DEBUG")]
        private static void Debug_EnsureMergedCorrectly(World src, World target)
        {
            validateEntities(src);
            validateEntities(target);

            if (src._entityEvents.Count > 0)
            {
                if (src._entities.Count != target._entities.Count)
                {
                    var exclusiveToSource = new HashSet<string>(src._entities.Keys);
                    exclusiveToSource.ExceptWith(target._entities.Keys);
                    var exclusiveToTarget = new HashSet<string>(target._entities.Keys);
                    exclusiveToTarget.ExceptWith(src._entities.Keys);

                    Debug.Assert(src._entities.Count == target._entities.Count);
                    Debug.Assert(src._nextEntityId == target._nextEntityId);
                }
            }

            static void validateEntities(World world)
            {
                foreach (var kvp in world._entities)
                {
                    Debug.Assert(world.IsEntityAlive(kvp.Value));
                }
            }
        }

        private static void ThrowCannotMerge()
            => throw new InvalidOperationException(
                "Instances of game state that have conflicting change sets cannot be merged. This is likely a bug.");

        private struct EntityEvent
        {
            public string EntityName;
            public string Alias;
            public Entity Entity;
            public EntityEventKind EventKind;
        }

        private enum EntityEventKind
        {
            EntityAdded,
            EntityRemoved,
            AliasAdded
        }

        private readonly struct AnimationEvent
        {
            public AnimationEvent(AnimationDictionaryKey key, AnimationEventKind kind)
            {
                Key = key;
                EventKind = kind;
            }

            public readonly AnimationDictionaryKey Key;
            public readonly AnimationEventKind EventKind;
        }

        private enum AnimationEventKind
        {
            AnimationActivated,
            AnimationDeactivated
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
                => HashHelper.Combine(Entity.GetHashCode(), RuntimeType.GetHashCode());
        }
    }
}
