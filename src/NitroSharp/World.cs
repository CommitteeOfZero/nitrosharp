using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using NitroSharp.Graphics;
using NitroSharp.Logic.Components;
using NitroSharp.Primitives;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp
{
    internal sealed partial class World
    {
        private const ushort InitialCapacity = 512;
        private const ushort InitialSpriteCount = 384;
        private const ushort InitialRectangleCount = 16;

        private readonly Dictionary<string, Entity> _entities;
        private readonly List<EntityTable> _tables;
        private ArrayBuilder<EntityEvent> _entityEvents;
        private uint _nextEntityId = 1;

        public Dictionary<Entity, AttachedBehavior<MoveAnimation>> _moveAnimations;
        public Dictionary<Entity, AttachedBehavior<FadeAnimation>> _fadeAnimations;
        public Dictionary<Entity, AttachedBehavior<ZoomAnimation>> _zoomAnimations;

        private readonly Queue<Entity> _detachedMoveAnimations;
        private readonly Queue<Entity> _detachedFadeAnimations;
        private readonly Queue<Entity> _detachedZoomAnimations;

        public World()
        {
            _entities = new Dictionary<string, Entity>(InitialCapacity);
            _entityEvents = new ArrayBuilder<EntityEvent>();
            _tables = new List<EntityTable>(8);

            Sprites = RegisterTable(new Sprites(InitialSpriteCount));
            Rectangles = RegisterTable(new Rectangles(InitialRectangleCount));

            _moveAnimations = new Dictionary<Entity, AttachedBehavior<MoveAnimation>>();
            _fadeAnimations = new Dictionary<Entity, AttachedBehavior<FadeAnimation>>();
            _zoomAnimations = new Dictionary<Entity, AttachedBehavior<ZoomAnimation>>();

            _detachedMoveAnimations = new Queue<Entity>();
            _detachedFadeAnimations = new Queue<Entity>();
            _detachedZoomAnimations = new Queue<Entity>();
        }

        public event Action<Entity> SpriteAdded;
        public event Action<Entity> SpriteRemoved;

        public Sprites Sprites { get; }
        public Rectangles Rectangles { get; }

        internal Dictionary<string, Entity>.Enumerator EntityEnumerator => _entities.GetEnumerator();

        private T RegisterTable<T>(T table) where T : EntityTable
        {
            _tables.Add(table);
            return table;
        }

        public T GetTable<T>(Entity entity) where T : EntityTable => (T)_tables[(int)entity.Kind];

        public bool TryGetEntity(string name, out Entity entity)
          => _entities.TryGetValue(name, out entity);

        public void SetAlias(string originalName, string alias)
        {
            if (TryGetEntity(originalName, out Entity entity))
            {
                _entities[alias] = entity;
            }
        }

        public Entity CreateSprite(string name, string image, in RectangleF sourceRectangle, int renderPriority, SizeF size, ref RgbaFloat color)
        {
            if (name == null) Debugger.Break();

            Entity entity = CreateVisual(name, EntityKind.Sprite, renderPriority, size, ref color);
            ref var meow = ref Sprites.SpriteComponents.Mutate(entity);
            meow.Image = image;
            meow.SourceRectangle = sourceRectangle;

            //SpriteAdded?.Invoke(entity);

            return entity;
        }

        public Entity CreateRectangle(string name, int renderPriority, SizeF size, ref RgbaFloat color)
        {
            Entity entity = CreateVisual(name, EntityKind.Rectangle, renderPriority, size, ref color);
            return entity;
        }

        private Entity CreateVisual(string name, EntityKind kind, int renderPriority, SizeF size, ref RgbaFloat color)
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

        public Entity CreateThread(string name)
        {
            return CreateEntity(name, EntityKind.Thread);
        }

        public ref MoveAnimation AttachMoveAnimation(Entity entity)
        {
            var behavior = _moveAnimations[entity] = new AttachedBehavior<MoveAnimation>();
            behavior.Entity = entity;
            return ref behavior.Behavior;
        }

        public ref FadeAnimation AttachFadeAnimation(Entity entity)
        {
            var behavior = _fadeAnimations[entity] = new AttachedBehavior<FadeAnimation>();
            behavior.Entity = entity;
            return ref behavior.Behavior;
        }

        public ref ZoomAnimation AttachZoomAnimation(Entity entity)
        {
            var behavior = _zoomAnimations[entity] = new AttachedBehavior<ZoomAnimation>();
            behavior.Entity = entity;
            return ref behavior.Behavior;
        }

        public IEnumerable<AttachedBehavior<MoveAnimation>> GetMoveAnimations() => _moveAnimations.Values;
        public IEnumerable<AttachedBehavior<FadeAnimation>> GetFadeAnimations() => _fadeAnimations.Values;
        public IEnumerable<AttachedBehavior<ZoomAnimation>> GetZoomAnimations() => _zoomAnimations.Values;

        public void DetachMoveAnimation(Entity entity) => _detachedMoveAnimations.Enqueue(entity);
        public void DetachFadeAnimation(Entity entity) => _detachedFadeAnimations.Enqueue(entity);
        public void DetachZoomAnimation(Entity entity) => _detachedZoomAnimations.Enqueue(entity);

        public void FlushDetachedBehaviors()
        {
            while (_detachedMoveAnimations.Count > 0)
            {
                RemoveBehavior(_moveAnimations, _detachedMoveAnimations.Dequeue());
            }

            while (_detachedFadeAnimations.Count > 0)
            {
                RemoveBehavior(_fadeAnimations, _detachedFadeAnimations.Dequeue());
            }

            while (_detachedZoomAnimations.Count > 0)
            {
                RemoveBehavior(_zoomAnimations, _detachedZoomAnimations.Dequeue());
            }
        }

        private void RemoveBehavior<T>(Dictionary<Entity, AttachedBehavior<T>> behaviors, Entity entity)
            where T : unmanaged
        {
            behaviors.Remove(entity);
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
        private Entity CreateEntityCore(string name, EntityKind kind)
        {
            EntityTable table = _tables[(int)kind];
            ushort index = table.ReserveColumn();
            var entity = new Entity(_nextEntityId++, kind, index);
            _entities[name] = entity;

            switch (kind)
            {
                case EntityKind.Sprite:
                    SpriteAdded?.Invoke(entity);
                    break;
            }

            return entity;
        }

        public void RemoveEntity(string name)
        {
            RemoveEntityCore(name);
            ref EntityEvent evt = ref _entityEvents.Add();
            evt.EntityName = name;
            evt.EventKind = EntityEventKind.EntityRemoved;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveEntityCore(string name, bool eraseCells = true)
        {
            if (_entities.TryGetValue(name, out Entity entity))
            {
                _entities.Remove(name);
                var table = GetTable<EntityTable>(entity);
                table.FreeColumn(entity, eraseCells);

                switch (entity.Kind)
                {
                    case EntityKind.Sprite:
                        SpriteRemoved?.Invoke(entity);
                        break;
                }
            }
        }

        public void CopyChanges(World anotherWorld)
        {
            Debug.Assert(_tables.Count == anotherWorld._tables.Count);
            for (int i = 0; i < _tables.Count; i++)
            {
                _tables[i].CopyChanges(anotherWorld._tables[i]);
            }

            anotherWorld._moveAnimations.Clear();
            foreach (var kvp in _moveAnimations)
            {
                anotherWorld._moveAnimations[kvp.Key] = kvp.Value;
            }
            anotherWorld._fadeAnimations.Clear();
            foreach (var kvp in _fadeAnimations)
            {
                anotherWorld._fadeAnimations[kvp.Key] = kvp.Value;
            }
            anotherWorld._zoomAnimations.Clear();
            foreach (var kvp in _zoomAnimations)
            {
                anotherWorld._zoomAnimations[kvp.Key] = kvp.Value;
            }

            for (int i = 0; i < _entityEvents.Count; i++)
            {
                ref EntityEvent evt = ref _entityEvents[i];
                if (evt.EventKind == EntityEventKind.EntityAdded)
                {
                    anotherWorld.CreateEntityCore(evt.EntityName, evt.Entity.Kind);
                }
                else
                {
                    anotherWorld.RemoveEntityCore(evt.EntityName, eraseCells: false);
                }
            }

            _entityEvents.Reset();
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
    }
}
