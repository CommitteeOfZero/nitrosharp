﻿using System;
using System.Collections.Generic;

namespace ProjectHoppy.Framework
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

        public bool HasComponent<T>() where T : Component => _components.ContainsKey(typeof(T));
        public bool HasComponent(Type type) => _components.ContainsKey(type);

        public T GetComponent<T>() where T : Component
        {
            if (_components.TryGetValue(typeof(T), out var collection))
            {
                return collection?.Count > 0 ? (T)collection[0] : null;
            }

            return null;
        }

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