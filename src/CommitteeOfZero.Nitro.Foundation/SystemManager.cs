using System;
using System.Collections.Generic;
using System.Linq;

namespace CommitteeOfZero.Nitro.Foundation
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
            if (_updatedEntities.Count > 0 || _removedEntities.Count > 0)
            {
                foreach (var system in _systems)
                {
                    (system as EntityProcessingSystem)?.RefreshLocalEntityList(_updatedEntities, _removedEntities);
                }
            }
            _entities.FlushRemovedComponents();
            _entities.FlushRemovedEntities();

            _updatedEntities.Clear();
            _removedEntities.Clear();

            foreach (var system in _systems)
            {
                system.Update(deltaMilliseconds);
            }
        }

        private void OnPreviewEntityUpdated(object sender, Entity e)
        {
            _updatedEntities.Add(e);
        }

        private void OnPreviewEntityRemoved(object sender, Entity e)
        {
            _removedEntities.Add(e);
            _updatedEntities.Remove(e);
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
