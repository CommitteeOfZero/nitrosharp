using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp
{
    internal sealed class OldEntityManager
    {
        private readonly Dictionary<string, OldEntity> _allEntities;
        private readonly HashSet<(OldEntity entity, Component component)> _componentsToRemove;
        private readonly HashSet<OldEntity> _entitiesToRemove;
        private uint _nextId;
        private readonly Stopwatch _gameTimer;

        private readonly Dictionary<string, string> _aliases;

        public OldEntityManager(Stopwatch gameTimer)
        {
            _allEntities = new Dictionary<string, OldEntity>(StringComparer.OrdinalIgnoreCase);
            _componentsToRemove = new HashSet<(OldEntity entity, Component component)>();
            _entitiesToRemove = new HashSet<OldEntity>();
            _gameTimer = gameTimer;

            _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, OldEntity> AllEntities => _allEntities;

        public event EventHandler<OldEntity> EntityUpdated;
        public event EventHandler<OldEntity> EntityDetached;
        public event EventHandler<OldEntity> EntityRemoved;
        public event EventHandler<OldEntity> EntityUpdateScheduled;
        public event EventHandler<OldEntity> EntityRemovalScheduled;

        public OldEntity Create(string name, bool replace = false)
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

            var newEntity = new OldEntity(this, _nextId++, name, _gameTimer.Elapsed);
            _allEntities[name] = newEntity;
            return newEntity;
        }

        public bool Exists(string name) => _allEntities.ContainsKey(name);
        public bool TryGet(string name, out OldEntity entity)
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

        public OldEntity Get(string name)
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
        /// Schedules the specified <see cref="OldEntity"/> to be removed on the next update.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(OldEntity entity) => Remove(entity.Name);

        /// <summary>
        /// Schedules the specified <see cref="OldEntity"/> to be removed on the next update.
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

        internal void ScheduleComponentRemoval(OldEntity entity, Component component)
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

        internal void InternalComponentAdded(OldEntity entity)
        {
            EntityUpdateScheduled?.Invoke(this, entity);
            EntityUpdated?.Invoke(this, entity);
        }
    }
}
