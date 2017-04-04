using System;
using System.Collections.Generic;
using System.Reflection;

namespace HoppyFramework
{
    public class Entity
    {
        private readonly EntityManager _manager;
        private readonly Dictionary<Type, IList<Component>> _components;

        internal Entity(EntityManager manager, ulong id, string name, TimeSpan creationTime)
        {
            _manager = manager;
            Id = id;
            Name = name;
            CreationTime = creationTime;

            _components = new Dictionary<Type, IList<Component>>();
        }

        public ulong Id { get; }
        public string Name { get; }
        public TimeSpan CreationTime { get; }

        public bool Locked { get; set; }

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
                collection = new List<Component>();
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

        public bool HasComponent<T>() where T : Component => GetComponent<T>() != null;
        public bool HasComponent(Type type) => GetComponent(type) != null;

        public object GetComponent(Type type)
        {
            if (_components.TryGetValue(type, out var collection))
            {
                return collection?.Count > 0 ? collection[0] : null;
            }
            else
            {
                foreach (var pair in _components)
                {
                    if (type.GetTypeInfo().IsAssignableFrom(pair.Key.GetTypeInfo()))
                    {
                        collection = pair.Value;
                        return collection?.Count > 0 ? collection[0] : null;
                    }
                }
            }

            return null;
        }

        public T GetComponent<T>() where T : Component => (T)GetComponent(typeof(T));

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
}
