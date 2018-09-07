using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Animation;
using NitroSharp.Dialogue;
using NitroSharp.Primitives;
using NitroSharp.Text;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp
{
    internal enum WorldKind
    {
        Primary,
        Secondary
    }

    internal sealed class World
    {
        public const ushort InitialCapacity = 512;
        public const ushort InitialSpriteCount = 384;
        public const ushort InitialRectangleCount = 16;
        public const ushort InitialTextLayoutCount = 32;

        private readonly Dictionary<string, Entity> _entities;
        private readonly List<EntityTable> _tables;
        private ArrayBuilder<EntityEvent> _entityEvents;
        private uint _nextEntityId = 1;
        
        private readonly Dictionary<BehaviorDictionaryKey, AttachedBehavior> _attachedBehaviors;
        private readonly List<(BehaviorDictionaryKey key, AttachedBehavior behavior)> _behaviorsToDetach;
        private readonly List<BehaviorEvent> _behaviorEvents;

        public DialogueState _dialogueState;
        public uint ActiveAnimationCount =>
            (uint)_attachedBehaviors.Count(x => x.Value is AnimationBase
            && !(x.Value is TextRevealAnimation) && !(x.Value is RevealSkipAnimation));

        public World(WorldKind kind)
        {
            Kind = kind;
            _entities = new Dictionary<string, Entity>(InitialCapacity);
            _entityEvents = new ArrayBuilder<EntityEvent>();
            _tables = new List<EntityTable>(8);

            Sprites = RegisterTable(new Sprites(InitialSpriteCount));
            Rectangles = RegisterTable(new Rectangles(InitialRectangleCount));
            TextInstances = RegisterTable(new TextInstances(InitialTextLayoutCount));

            _attachedBehaviors = new Dictionary<BehaviorDictionaryKey, AttachedBehavior>();
            _behaviorsToDetach = new List<(BehaviorDictionaryKey key, AttachedBehavior behavior)>();
            _behaviorEvents = new List<BehaviorEvent>();
        }

        public WorldKind Kind { get; }
        public bool IsPrimary => Kind == WorldKind.Primary;

        public Sprites Sprites { get; }
        public Rectangles Rectangles { get; }
        public TextInstances TextInstances { get; }

        public DialogueState DialogueState => _dialogueState;

        public event Action<Entity> SpriteAdded;
        public event Action<Entity> SpriteRemoved;
        public event Action<Entity> TextInstanceAdded;
        public event Action<Entity> TextInstanceRemoved;

        public Dictionary<string, Entity>.Enumerator EntityEnumerator => _entities.GetEnumerator();
        public Dictionary<BehaviorDictionaryKey, AttachedBehavior>.ValueCollection AttachedBehaviors
            => _attachedBehaviors.Values;

        private T RegisterTable<T>(T table) where T : EntityTable
        {
            _tables.Add(table);
            return table;
        }

        public T GetTable<T>(Entity entity) where T : EntityTable
            => (T)_tables[(int)entity.Kind];

        public bool TryGetEntity(string name, out Entity entity)
            => _entities.TryGetValue(name, out entity);

        public void SetAlias(string originalName, string alias)
        {
            if (TryGetEntity(originalName, out Entity entity))
            {
                _entities[alias] = entity;
            }
        }

        public Entity CreateSprite(
            string name, string image, in RectangleF sourceRectangle,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.Sprite, renderPriority, size, ref color);
            ref var meow = ref Sprites.SpriteComponents.Mutate(entity);
            meow.Image = image;
            meow.SourceRectangle = sourceRectangle;

            RaiseEventIfPrimary(EntityEventKind.EntityAdded, entity);
            return entity;
        }

        public Entity CreateRectangle(string name, int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.Rectangle, renderPriority, size, ref color);
            RaiseEventIfPrimary(EntityEventKind.EntityAdded, entity);
            return entity;
        }

        public Entity CreateTextInstance(string name, TextLayout layout, int renderPriority, ref RgbaFloat color)
        {
            var bounds = new SizeF(layout.MaxBounds.Width, layout.MaxBounds.Height);
            Entity entity = CreateVisual(name, EntityKind.Text, renderPriority, bounds, ref color);
            TextInstances.Layouts.Set(entity, ref layout);
            TextInstances.ClearFlags.Set(entity, true);

            RaiseEventIfPrimary(EntityEventKind.EntityAdded, entity);
            return entity;
        }

        private Entity CreateVisual(
            string name, EntityKind kind,
            int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateEntity(name, kind);
            Visuals table = GetTable<Visuals>(entity);

            if (renderPriority > 0)
            {
                renderPriority += (int)entity.UniqueId;
            }

            table.RenderPriorities.Set(entity, renderPriority);
            table.Bounds.Set(entity, size);
            table.Colors.Set(entity, ref color);
            table.TransformComponents.Mutate(entity).Scale = Vector3.One;
            return entity;
        }

        public void ActivateBehavior<T>(T behavior) where T : AttachedBehavior
        {
            var key = new BehaviorDictionaryKey(behavior.Entity, typeof(T));
            _attachedBehaviors[key] = behavior;
            _behaviorEvents.Add(new BehaviorEvent(key, BehaviorEvenKind.BehaviorActivated));
        }

        public void DeactivateBehavior(AttachedBehavior behavior)
        {
            var key = new BehaviorDictionaryKey(behavior.Entity, behavior.GetType());
            _behaviorsToDetach.Add((key, behavior));
            _behaviorEvents.Add(new BehaviorEvent(key, BehaviorEvenKind.BehaviorDeactivated));
        }

        public bool TryGetBehavior<T>(Entity entity, out T behavior) where T : AttachedBehavior
        {
            var key = new BehaviorDictionaryKey(entity, typeof(T));
            bool result = _attachedBehaviors.TryGetValue(key, out AttachedBehavior val);
            behavior = val as T;
            return result;
        }

        public void FlushDetachedBehaviors()
        {
            foreach ((var dictKey, var behavior) in _behaviorsToDetach)
            {
                if (_attachedBehaviors.TryGetValue(dictKey, out var value) && value == behavior)
                {
                    _attachedBehaviors.Remove(dictKey);
                }
            }
            _behaviorsToDetach.Clear();
        }

        private Entity CreateEntity(string name, EntityKind kind)
        {
            Entity handle = CreateEntityCore(name, kind);
            ref EntityEvent evt = ref _entityEvents.Add();
            evt.Entity = handle;
            evt.EntityName = name;
            evt.EventKind = EntityEventKind.EntityAdded;
            return handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity CreateEntityCore(string name, EntityKind kind, bool reserveColumn = true)
        {
            EntityTable table = _tables[(int)kind];
            ushort index = table.ReserveColumn();
            var entity = new Entity(_nextEntityId++, kind, index);
            _entities[name] = entity;
            return entity;
        }

        public void RemoveEntity(string name)
        {
            Entity e = RemoveEntityCore(name);
            ref EntityEvent evt = ref _entityEvents.Add();
            evt.EntityName = name;
            evt.Entity = e;
            evt.EventKind = EntityEventKind.EntityRemoved;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Entity RemoveEntityCore(string name, bool eraseCells = true)
        {
            if (_entities.TryGetValue(name, out Entity entity))
            {
                _entities.Remove(name);
                var table = GetTable<EntityTable>(entity);
                table.FreeColumn(entity, eraseCells);
                RaiseEventIfPrimary(EntityEventKind.EntityRemoved, entity);
                return entity;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RaiseEventIfPrimary(EntityEventKind eventKind, Entity entity)
        {
            if (Kind == WorldKind.Primary)
            {
                if (eventKind == EntityEventKind.EntityAdded)
                {
                    RaiseEntityCreated(entity);
                }
                else
                {
                    RaiseEntityRemoved(entity);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RaiseEntityCreated(Entity entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Sprite:
                    SpriteAdded?.Invoke(entity);
                    break;
                case EntityKind.Text:
                    TextInstanceAdded?.Invoke(entity);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RaiseEntityRemoved(Entity entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Sprite:
                    SpriteRemoved?.Invoke(entity);
                    break;
                case EntityKind.Text:
                    TextInstanceRemoved?.Invoke(entity);
                    break;
            }
        }

        public void CopyChanges(World anotherWorld)
        {
            Debug.Assert(_tables.Count == anotherWorld._tables.Count);
            for (int i = 0; i < _tables.Count; i++)
            {
                _tables[i].CopyChanges(anotherWorld._tables[i]);
            }

            foreach (BehaviorEvent be in _behaviorEvents)
            {
                if (be.EventKind == BehaviorEvenKind.BehaviorActivated)
                {
                    if (_attachedBehaviors.TryGetValue(be.Key, out AttachedBehavior behavior))
                    {
                        anotherWorld._attachedBehaviors[be.Key] = behavior;
                    }
                }
                else
                {
                    anotherWorld._attachedBehaviors.Remove(be.Key);
                }
            }

            _behaviorEvents.Clear();

            for (int i = 0; i < _entityEvents.Count; i++)
            {
                ref EntityEvent evt = ref _entityEvents[i];
                if (evt.EventKind == EntityEventKind.EntityAdded)
                {
                    anotherWorld._entities[evt.EntityName] = evt.Entity;
                }
                else
                {
                    anotherWorld._entities.Remove(evt.EntityName);
                }

                anotherWorld.RaiseEventIfPrimary(evt.EventKind, evt.Entity);
            }

            _entityEvents.Reset();
            anotherWorld._dialogueState = _dialogueState;
        }

        private struct EntityEvent
        {
            public string EntityName;
            public Entity Entity;
            public EntityEventKind EventKind;
        }

        private enum EntityEventKind
        {
            EntityAdded,
            EntityRemoved
        }

        private readonly struct BehaviorEvent
        {
            public BehaviorEvent(BehaviorDictionaryKey key, BehaviorEvenKind kind)
            {
                Key = key;
                EventKind = kind;
            }

            public readonly BehaviorDictionaryKey Key;
            public readonly BehaviorEvenKind EventKind;
        }

        private enum BehaviorEvenKind
        {
            BehaviorActivated,
            BehaviorDeactivated
        }

        internal readonly struct BehaviorDictionaryKey : IEquatable<BehaviorDictionaryKey>
        {
            public readonly Entity Entity;
            public readonly Type RuntimeType;

            public BehaviorDictionaryKey(Entity entity, Type runtimeType)
            {
                Entity = entity;
                RuntimeType = runtimeType;
            }

            public bool Equals(BehaviorDictionaryKey other)
                => Entity.Equals(other.Entity) && RuntimeType.Equals(other.RuntimeType);

            public override bool Equals(object obj)
                => obj is BehaviorDictionaryKey other && Equals(other);

            public override int GetHashCode()
                => HashHelper.Combine(Entity.GetHashCode(), RuntimeType.GetHashCode());
        }
    }
}
