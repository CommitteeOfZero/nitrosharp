using System;
using System.Collections.Generic;

namespace MoeGame.Framework
{
    public abstract class EntityProcessingSystem : GameSystem
    {
        private readonly HashSet<Entity> _entities;
        private readonly HashSet<Type> _interests;

        public event EventHandler<Entity> RelevantEntityAdded;
        public event EventHandler<Entity> RelevantEntityRemoved;

        protected EntityProcessingSystem()
        {
            _entities = new HashSet<Entity>();
            _interests = new HashSet<Type>();
            DeclareInterests(_interests);
        }

        protected abstract void DeclareInterests(ISet<Type> interests);

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
                if (_entities.Remove(removed))
                {
                    RelevantEntityRemoved?.Invoke(this, removed);
                }
            }

            foreach (var updated in updatedEntities)
            {
                EntityChanged(updated);
            }
        }

        private void EntityChanged(Entity entity)
        {
            if (IsRelevant(entity) && _entities.Add(entity))
            {
                RelevantEntityAdded?.Invoke(this, entity);
            }
        }

        private bool IsRelevant(Entity entity)
        {
            foreach (Type interest in _interests)
            {
                if (entity.HasComponent(interest))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
