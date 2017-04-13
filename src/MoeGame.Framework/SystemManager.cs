using System;
using System.Collections.Generic;
using System.Linq;

namespace MoeGame.Framework
{
    public class SystemManager : IDisposable
    {
        private readonly List<GameSystem> _systems;

        private readonly HashSet<Entity> _updatedEntities;
        private readonly HashSet<Entity> _removedEntities;

        public SystemManager(EntityManager entities)
        {
            _systems = new List<GameSystem>();

            _updatedEntities = new HashSet<Entity>();
            _removedEntities = new HashSet<Entity>();

            entities.EntityUpdated += OnEntityUpdated;
            entities.EntityRemoved += OnEntityRemoved;
        }

        public IEnumerable<GameSystem> All => _systems;

        internal void Add(GameSystem system)
        {
            _systems.Add(system);
        }

        public void Update(float deltaMilliseconds)
        {
            foreach (var system in _systems)
            {
                if (_updatedEntities.Count > 0 || _removedEntities.Count > 0)
                {
                    (system as EntityProcessingSystem)?.RefreshLocalEntityList(_updatedEntities, _removedEntities);
                }

                system.Update(deltaMilliseconds);
            }

            _updatedEntities.Clear();
            _removedEntities.Clear();
        }

        private void OnEntityUpdated(object sender, Entity e)
        {
            _updatedEntities.Add(e);
        }

        private void OnEntityRemoved(object sender, Entity e)
        {
            _removedEntities.Add(e);
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
