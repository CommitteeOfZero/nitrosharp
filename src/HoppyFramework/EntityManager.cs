using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HoppyFramework
{
    public class EntityManager
    {
        private readonly Dictionary<string, Entity> _allEntities;
        private ulong _nextId;
        private readonly Stopwatch _gameTimer;

        public EntityManager(Stopwatch gameTimer)
        {
            _allEntities = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
            _gameTimer = gameTimer;
        }

        public IReadOnlyDictionary<string, Entity> AllEntities => _allEntities;

        public event EventHandler<Entity> EntityUpdated;
        public event EventHandler<Entity> EntityRemoved;

        public Entity Create(string name, bool replace = false)
        {
            if (_allEntities.ContainsKey(name))
            {
                if (replace)
                {
                    Remove(name);
                }
                else
                {
                    throw new InvalidOperationException($"Entity '{name}' already exists.");
                }
            }

            var entity = new Entity(this, _nextId++, name, _gameTimer.Elapsed);
            _allEntities[name] = entity;
            return entity;
        }

        public bool Exists(string name) => _allEntities.ContainsKey(name);

        public Entity Get(string name)
        {
            if (!_allEntities.TryGetValue(name, out var entity))
            {
                throw new ArgumentException($"Entity '{name}' does not exist.");
            }

            return entity;
        }

        public Entity SafeGet(string name)
        {
            if (!_allEntities.TryGetValue(name, out var entity))
            {
                return null;
            }

            return entity;
        }

        public void Remove(Entity entity) => Remove(entity.Name);
        public void Remove(string entityName)
        {
            if (_allEntities.TryGetValue(entityName, out var entity))
            {
                _allEntities.Remove(entityName);
                EntityRemoved?.Invoke(this, entity);
            }
        }

        internal void RaiseEntityUpdated(Entity entity)
        {
            EntityUpdated?.Invoke(this, entity);
        }
    }
}
