using System;
using System.Collections.Generic;
using System.Linq;

namespace NitroSharp.Foundation
{
    public class SystemManager : IDisposable
    {
        private readonly List<GameSystem> _systems;
        private readonly EntityManager _entities;

        private readonly HashSet<Entity> _updatedEntities;
        private readonly HashSet<Entity> _removedEntities;

        public SystemManager(EntityManager entities)
        {
            _systems = new List<GameSystem>();
            _entities = entities;

            _updatedEntities = new HashSet<Entity>();
            _removedEntities = new HashSet<Entity>();

            entities.EntityUpdateScheduled += OnPreviewEntityUpdated;
            entities.EntityRemovalScheduled += OnPreviewEntityRemoved;
        }

        public IEnumerable<GameSystem> All => _systems;

        internal void Add(GameSystem system)
        {
            _systems.Add(system);
        }

        public void Update(float deltaMilliseconds)
        {
            while (_updatedEntities.Count > 0 || _removedEntities.Count > 0)
            {
                var updated = _updatedEntities.ToArray();
                var removed = _removedEntities.ToArray();

                _updatedEntities.Clear();
                _removedEntities.Clear();

                foreach (var system in _systems)
                {
                    (system as EntityProcessingSystem)?.RefreshLocalEntityList(updated, removed);
                }
            }
            _entities.FlushRemovedComponents();
            _entities.FlushRemovedEntities();

            //_updatedEntities.Clear();
            //_removedEntities.Clear();

            foreach (var system in _systems)
            {
                system.Update(deltaMilliseconds);
            }
        }

        private void OnPreviewEntityUpdated(object sender, Entity entity)
        {
            if (!_removedEntities.Contains(entity))
            {
                _updatedEntities.Add(entity);
            }
        }

        private void OnPreviewEntityRemoved(object sender, Entity entity)
        {
            _removedEntities.Add(entity);
            _updatedEntities.Remove(entity);
        }

        public void Dispose()
        {
            foreach (var disposable in _systems.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }
    }
}
