using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoeGame.Framework
{
    public sealed class Entity
    {
        private readonly EntityManager _manager;
        private readonly Dictionary<Type, IList<Component>> _components;
        private Dictionary<string, object> _additionalProperties;

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

        public Dictionary<string, object> AdditionalProperties
        {
            get
            {
                if (_additionalProperties == null)
                {
                    _additionalProperties = new Dictionary<string, object>();
                }

                return _additionalProperties;
            }
        }

        internal Dictionary<Type, IList<Component>> Components => _components;

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
            }
            else
            {
                foreach (var pair in _components)
                {
                    if (type.GetTypeInfo().IsAssignableFrom(pair.Key.GetTypeInfo()))
                    {
                        collection = pair.Value;
                        if (collection?.Remove(component) == true)
                        {
                            break;
                        }
                    }
                }
            }

            //component.OnRemoved();
            _manager.RaiseEntityUpdated(this);
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
                        if (collection?.Count > 0)
                        {
                            return collection[0];
                        }
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

        public override string ToString()
        {
            return Name;
        }
    }
}
