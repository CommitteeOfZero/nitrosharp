using System;
using System.Collections.Generic;
using System.Linq;

namespace NitroSharp
{
    internal sealed class OldSystemManager : IDisposable
    {
        private readonly List<OldGameSystem> _systems;
        private readonly OldEntityManager _entities;

        private readonly HashSet<OldEntity> _updatedEntities;
        private readonly HashSet<OldEntity> _removedEntities;

        public OldSystemManager(OldEntityManager entities)
        {
            _systems = new List<OldGameSystem>();
            _entities = entities;

            _updatedEntities = new HashSet<OldEntity>();
            _removedEntities = new HashSet<OldEntity>();

            entities.EntityUpdateScheduled += OnPreviewEntityUpdated;
            entities.EntityRemovalScheduled += OnPreviewEntityRemoved;
        }

        public IEnumerable<OldGameSystem> All => _systems;

        internal void Add(OldGameSystem system)
        {
            _systems.Add(system);
        }

        public void ProcessEntityUpdates()
        {
            if (_updatedEntities.Count > 0 || _removedEntities.Count > 0)
            {
                foreach (var system in _systems)
                {
                    (system as OldEntityProcessingSystem)?.ProcessEntityUpdates(_updatedEntities, _removedEntities);
                }
            }

            _entities.FlushRemovedComponents();
            _entities.FlushRemovedEntities();

            _updatedEntities.Clear();
            _removedEntities.Clear();
        }

        public void Update(float deltaMilliseconds)
        {
            foreach (var system in _systems)
            {
                system.Update(deltaMilliseconds);
            }
        }

        private void OnPreviewEntityUpdated(object sender, OldEntity entity)
        {
            if (!_removedEntities.Contains(entity))
            {
                _updatedEntities.Add(entity);
            }
        }

        private void OnPreviewEntityRemoved(object sender, OldEntity entity)
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
