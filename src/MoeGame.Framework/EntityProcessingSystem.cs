using System;
using System.Collections.Generic;

namespace MoeGame.Framework
{
    public abstract class EntityProcessingSystem : GameSystem
    {
        private readonly HashSet<Entity> _entities;
        private readonly HashSet<Type> _interests;

        protected EntityProcessingSystem()
        {
            _entities = new HashSet<Entity>();
            _interests = new HashSet<Type>();
            DeclareInterests(_interests);
        }

        public IEnumerable<Type> Interests => _interests;

        protected abstract void DeclareInterests(ISet<Type> interests);

        public override void Update(float deltaMilliseconds)
        {
            ProcessAll(_entities, deltaMilliseconds);
        }

        public virtual IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities;
        }

        public abstract void Process(Entity entity, float deltaMilliseconds);
        public void ProcessAll(IEnumerable<Entity> entities, float deltaMilliseconds)
        {
            foreach (var item in SortEntities(entities))
            {
                Process(item, deltaMilliseconds);
            }
        }


        public virtual void OnRelevantEntityAdded(Entity entity)
        {
        }

        public virtual void OnRelevantEntityRemoved(Entity entity)
        {
        }

        internal void RefreshLocalEntityList(IEnumerable<Entity> updatedEntities, IEnumerable<Entity> removedEntities)
        {
            foreach (var removed in removedEntities)
            {
                if (_entities.Remove(removed))
                {
                    OnRelevantEntityRemoved(removed);
                }
            }

            foreach (var updated in updatedEntities)
            {
                EntityUpdated(updated);
            }
        }

        private void EntityUpdated(Entity entity)
        {
            if (IsRelevant(entity) && _entities.Add(entity))
            {
                OnRelevantEntityAdded(entity);
            }
        }

        private bool IsRelevant(Entity entity)
        {
            if (entity.Components.Values.Count == 0)
            {
                return false;
            }

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
