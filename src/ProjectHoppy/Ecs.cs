using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectHoppy
{
    public class Entity
    {
        private readonly EntityManager _manager;
        private readonly Dictionary<Type, ICollection<Component>> _components;

        internal Entity(EntityManager manager, ulong id, string name)
        {
            _manager = manager;
            Id = id;
            Name = name;

            _components = new Dictionary<Type, ICollection<Component>>();
        }

        public ulong Id { get; }
        public string Name { get; }

        public Entity WithComponent<T>(T component) where T : Component
        {
            AddComponent(component);
            return this;
        }

        public void AddComponent<T>(T component) where T : Component
        {
            var type = typeof(T);
            if (!_components.TryGetValue(type, out var collection))
            {
                collection = new HashSet<Component>();
                _components.Add(type, collection);
            }

            collection.Add(component);
            _manager.RaiseEntityUpdated(this);
        }

        public void RemoveComponent<T>(T component) where T : Component
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var collection))
            {
                collection.Remove(component);
                _manager.RaiseEntityUpdated(this);
            }
        }

        public bool HasComponent<T>() where T : Component => _components.ContainsKey(typeof(T));
        public bool HasComponent(Type type) => _components.ContainsKey(type);

        public T GetComponent<T>() where T : Component => GetComponents<T>().FirstOrDefault();
        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            if (_components.TryGetValue(typeof(T), out var collection))
            {
                foreach (var component in collection)
                {
                    yield return (T)component;
                }
            }
        }
    }

    public abstract class Component
    {
        public bool IsEnabled { get; set; } = true;
    }

    public class EntityManager
    {
        private readonly Dictionary<string, Entity> _allEntities;
        private ulong _nextId;

        public EntityManager()
        {
            _allEntities = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
        }

        public event EventHandler<Entity> EntityUpdated;
        public event EventHandler<Entity> EntityRemoved;

        public Entity CreateEntity(string name)
        {
            if (_allEntities.ContainsKey(name))
            {
                throw new InvalidOperationException($"Entity '{name}' already exists.");
            }

            var entity = new Entity(this, _nextId++, name);
            _allEntities[name] = entity;
            return entity;
        }

        public void RemoveEntity(Entity entity)
        {
            _allEntities.Remove(entity.Name);
            EntityRemoved?.Invoke(this, entity);
        }

        internal void RaiseEntityUpdated(Entity entity)
        {
            EntityUpdated?.Invoke(this, entity);
        }
    }

    public abstract class System
    {
        public abstract void Update(float deltaMilliseconds);
    }

    public abstract class EntityProcessingSystem : System
    {
        private readonly List<Entity> _entities;
        private readonly HashSet<Type> _interests;

        protected EntityProcessingSystem(params Type[] interests)
        {
            _entities = new List<Entity>();
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

        public virtual void OnEnityAdded(Entity e)
        {
        }

        public virtual void OnEntityRemoved(Entity e)
        {
        }

        internal void RefreshLocalEntityList(IEnumerable<Entity> updatedEntities, IEnumerable<Entity> removedEntities)
        {
            foreach (var updated in updatedEntities)
            {
                EntityChanged(updated);
            }

            foreach (var removed in removedEntities)
            {
                _entities.Remove(removed);
                OnEntityRemoved(removed);
            }
        }

        private void EntityChanged(Entity entity)
        {
            foreach (Type interest in _interests)
            {
                if (entity.HasComponent(interest))
                {
                    _entities.Add(entity);
                    OnEnityAdded(entity);
                    break;
                }
            }
        }
    }

    public class SystemManager : IDisposable
    {
        private readonly List<System> _systems;

        private readonly HashSet<Entity> _updatedEntities;
        private readonly HashSet<Entity> _removedEntities;

        public SystemManager(EntityManager entities)
        {
            _systems = new List<System>();

            _updatedEntities = new HashSet<Entity>();
            _removedEntities = new HashSet<Entity>();

            entities.EntityUpdated += OnEntityUpdated;
            entities.EntityRemoved += OnEntityRemoved;
        }

        public IEnumerable<System> All => _systems;

        public void RegisterSystem(System system)
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
