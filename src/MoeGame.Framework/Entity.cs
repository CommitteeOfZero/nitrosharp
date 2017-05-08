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

        public bool HasComponent<T>() where T : Component => GetComponent<T>() != null;
        public bool HasComponent(Type type) => GetComponent(type) != null;

        public T GetComponent<T>() where T : Component => (T)GetComponent(typeof(T));
        public object GetComponent(Type type)
        {
            var componentBag = GetComponentBag(type);
            return componentBag?.Count > 0 ? componentBag[0] : null;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            var bag = GetComponentBag(typeof(T));
            if (bag != null)
            {
                foreach (var component in GetComponentBag(typeof(T)))
                {
                    yield return (T)component;
                }
            }
        }

        /// <summary>
        /// Schedules the specified component to be removed on the next update.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        public void RemoveComponent<T>(T component) where T : Component
        {
            _manager.ScheduleComponentRemoval(this, component);
        }

        internal void CommitDestroyComponent(Component component)
        {
            var type = component.GetType();
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

            _manager.RaiseEntityUpdated(this);
        }

        private IList<Component> GetComponentBag(Type type)
        {
            if (_components.TryGetValue(type, out var collection))
            {
                return collection?.Count > 0 ? collection : null;
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
                            return collection;
                        }
                    }
                }
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
