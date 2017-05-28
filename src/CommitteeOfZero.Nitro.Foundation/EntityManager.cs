using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CommitteeOfZero.Nitro.Foundation
{
    public class EntityManager
    {
        private readonly Dictionary<string, Entity> _allEntities;
        private readonly HashSet<(Entity entity, Component component)> _componentsToRemove;
        private readonly HashSet<Entity> _entitiesToRemove;
        private ulong _nextId;
        private readonly Stopwatch _gameTimer;

        public EntityManager(Stopwatch gameTimer)
        {
            _allEntities = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
            _componentsToRemove = new HashSet<(Entity entity, Component component)>();
            _entitiesToRemove = new HashSet<Entity>();
            _gameTimer = gameTimer;
        }

        public IReadOnlyDictionary<string, Entity> AllEntities => _allEntities;

        public event EventHandler<Entity> EntityUpdated;
        public event EventHandler<Entity> EntityRemoved;

        public void Add(string name, Entity entity)
        {
            _allEntities[name] = entity;
        }

        public Entity Create(string name, bool replace = false)
        {
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
        public bool TryGet(string name, out Entity entity) => _allEntities.TryGetValue(name, out entity);

        public Entity Get(string name)
        {
            if (!TryGet(name, out var entity))
            {
                throw new ArgumentException($"Entity '{name}' does not exist.");
            }

            return entity;
        }

        /// <summary>
        /// Schedules the specified <see cref="Entity"/> to be removed on the next update.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(Entity entity) => Remove(entity.Name);

        /// <summary>
        /// Schedules the specified <see cref="Entity"/> to be removed on the next update.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(string entityName)
        {
            if (_allEntities.TryGetValue(entityName, out var entity))
            {
                _entitiesToRemove.Add(entity);
            }
        }

        internal void ScheduleComponentRemoval(Entity entity, Component component)
        {
            _componentsToRemove.Add((entity, component));
        }

        internal void FlushRemovedComponents()
        {
            foreach (var tuple in _componentsToRemove)
            {
                tuple.entity.CommitDestroyComponent(tuple.component);
            }

            _componentsToRemove.Clear();
        }

        internal void FlushRemovedEntities()
        {
            foreach (var entity in _entitiesToRemove)
            {
                CommitRemoveEntity(entity);
            }

            _entitiesToRemove.Clear();
        }

        internal void RaiseEntityUpdated(Entity entity)
        {
            EntityUpdated?.Invoke(this, entity);
        }

        private void CommitRemoveEntity(Entity entity)
        {
            if (_allEntities.TryGetValue(entity.Name, out var currentValue))
            {
                if (entity == currentValue)
                {
                    _allEntities.Remove(entity.Name);
                }
            }

            EntityRemoved?.Invoke(this, entity);
        }
    }
}
