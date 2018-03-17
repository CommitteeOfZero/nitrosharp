using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp
{
    internal sealed class EntityManager
    {
        private readonly Dictionary<string, Entity> _allEntities;
        private readonly HashSet<(Entity entity, Component component)> _componentsToRemove;
        private readonly HashSet<Entity> _entitiesToRemove;
        private uint _nextId;
        private readonly Stopwatch _gameTimer;

        private readonly Dictionary<string, string> _aliases;

        public EntityManager(Stopwatch gameTimer)
        {
            _allEntities = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
            _componentsToRemove = new HashSet<(Entity entity, Component component)>();
            _entitiesToRemove = new HashSet<Entity>();
            _gameTimer = gameTimer;

            _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, Entity> AllEntities => _allEntities;

        public event EventHandler<Entity> EntityUpdated;
        public event EventHandler<Entity> EntityRemoved;
        public event EventHandler<Entity> EntityUpdateScheduled;
        public event EventHandler<Entity> EntityRemovalScheduled;

        public Entity Create(string name, bool replace = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "Cannot create an entity with an empty name.");
            }

            if (_allEntities.TryGetValue(name, out var existingEntity))
            {
                if (replace)
                {
                    Remove(existingEntity);
                }
                else
                {
                    throw new InvalidOperationException($"Entity '{name}' already exists.");
                }
            }

            var newEntity = new Entity(this, _nextId++, name, _gameTimer.Elapsed);
            _allEntities[name] = newEntity;
            return newEntity;
        }

        public bool Exists(string name) => _allEntities.ContainsKey(name);
        public bool TryGet(string name, out Entity entity)
        {
            bool exists = _allEntities.TryGetValue(name, out entity);
            if (!exists)
            {
                if (_aliases.TryGetValue(name, out string actualName))
                {
                    return TryGet(actualName, out entity);
                }
            }

            return exists;
        }

        public Entity Get(string name)
        {
            if (!TryGet(name, out var entity))
            {
                throw new ArgumentException($"Entity '{name}' does not exist.");
            }

            return entity;
        }

        internal void SetAlias(string entityName, string alias)
        {
            _aliases[alias] = entityName;
        }

        /// <summary>
        /// Schedules the specified <see cref="Entity"/> to be removed on the next update.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(Entity entity) => Remove(entity.Name);

        /// <summary>
        /// Schedules the specified <see cref="Entity"/> to be removed on the next update.
        /// </summary>
        public void Remove(string entityName)
        {
            if (_allEntities.TryGetValue(entityName, out var entity))
            {
                entity.IsScheduledForRemoval = true;
                _entitiesToRemove.Add(entity);
                EntityRemovalScheduled?.Invoke(this, entity);
            }
        }

        internal void ScheduleComponentRemoval(Entity entity, Component component)
        {
            _componentsToRemove.Add((entity, component));
            component.IsScheduledForRemoval = true;
            EntityUpdateScheduled?.Invoke(this, entity);
        }

        internal void FlushRemovedComponents()
        {
            foreach (var tuple in _componentsToRemove)
            {
                tuple.entity.CommitDestroyComponent(tuple.component);
                EntityUpdated?.Invoke(this, tuple.entity);
            }

            _componentsToRemove.Clear();
        }

        internal void FlushRemovedEntities()
        {
            foreach (var entity in _entitiesToRemove)
            {
                if (_allEntities.TryGetValue(entity.Name, out var currentValue))
                {
                    if (entity == currentValue)
                    {
                        _allEntities.Remove(entity.Name);
                    }
                }

                entity.CommitDestroy();
                EntityRemoved?.Invoke(this, entity);
            }

            _entitiesToRemove.Clear();
        }

        internal void InternalComponentAdded(Entity entity)
        {
            EntityUpdateScheduled?.Invoke(this, entity);
            EntityUpdated?.Invoke(this, entity);
        }
    }
}
