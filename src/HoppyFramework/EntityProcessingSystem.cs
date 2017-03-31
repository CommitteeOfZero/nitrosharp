using System;
using System.Collections.Generic;

namespace HoppyFramework
{
    public abstract class EntityProcessingSystem : GameSystem
    {
        private readonly HashSet<Entity> _entities;
        private readonly HashSet<Type> _interests;

        public event EventHandler<Entity> EntityAdded;
        public event EventHandler<Entity> EntityRemoved;

        protected EntityProcessingSystem(params Type[] interests)
        {
            _entities = new HashSet<Entity>();
            _interests = new HashSet<Type>(interests);
        }

        public override void Update(float deltaMilliseconds)
        {
            ProcessAll(_entities, deltaMilliseconds);
        }

        public virtual IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities;
        }
        
        public void ProcessAll(IEnumerable<Entity> entities, float deltaMilliseconds)
        {
            foreach (var item in SortEntities(entities))
            {
                Process(item, deltaMilliseconds);
            }
        }

        public abstract void Process(Entity entity, float deltaMilliseconds);

        internal void RefreshLocalEntityList(IEnumerable<Entity> updatedEntities, IEnumerable<Entity> removedEntities)
        {
            foreach (var removed in removedEntities)
            {
                _entities.Remove(removed);
                EntityRemoved?.Invoke(this, removed);
            }

            foreach (var updated in updatedEntities)
            {
                EntityChanged(updated);
            }
        }

        private void EntityChanged(Entity entity)
        {
            foreach (Type interest in _interests)
            {
                if (entity.HasComponent(interest))
                {
                    if (_entities.Add(entity))
                    {
                        EntityAdded?.Invoke(this, entity);
                    }
                    break;
                }
            }
        }
    }
}
