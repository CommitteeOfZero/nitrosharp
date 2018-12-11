using System;
using NitroSharp.EntitySystem;
using Xunit;

namespace NitroSharp.Tests
{
    public class GameWorldTests
    {
        private const string Foo = "foo";
        private readonly GameWorld _world = new GameWorld(GameWorldConfiguration.Default);

        [Fact]
        public void CreateAndRequestEntity()
        {
            Entity entity = _world.CreateEntity(Foo);
            Assert.Equal(entity, _world.GetEntity(Foo));
        }

        [Fact]
        public void CreateEntity_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => _world.CreateEntity(null));
        }

        [Fact]
        public void CreateEntity_EmptyName()
        {
            Assert.Throws<ArgumentNullException>(() => _world.CreateEntity(""));
        }

        [Fact]
        public void Entity_IDs_StartFrom_1()
        {
            Entity foo = _world.CreateEntity(Foo);
            Assert.Equal(1, foo.Id);
        }

        [Fact]
        public void ExhaustEntityIDs()
        {
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                _world.CreateEntity(i.ToString());
            }

            Assert.Throws<InvalidOperationException>(() => _world.CreateEntity(Foo));
        }

        [Fact]
        public void LookupTables_Grow_AsNecessary()
        {
            Entity last = default;
            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                last = _world.CreateEntity(i.ToString());
            }

            // Add a component to the 65535th entity.
            _world.AddComponent<TestComponent>(last);
        }

        [Fact]
        public void ReplaceExistingEntity_WhenAllowed()
        {
            Entity original = _world.CreateEntity(Foo);
            Entity @new = _world.CreateEntity(Foo);
            Assert.NotEqual(original, @new);
            Assert.Equal(@new, _world.GetEntity(Foo));
        }

        [Fact]
        public void ReplaceExistingEntity_WhenDisallowed()
        {
            Entity original = _world.CreateEntity(Foo);
            Assert.Throws<InvalidOperationException>(() => _world.CreateEntity(Foo, replaceExisting: false));
        }

        [Fact]
        public void RequestNonExistentEntity()
        {
            Assert.Throws<InvalidOperationException>(() => _world.GetEntity(Foo));
        }

        [Fact]
        public void RequestDestroyedEntity()
        {
            Entity entity = _world.CreateEntity(Foo);
            _world.DestroyEntity(Foo);
            Assert.Throws<InvalidOperationException>(() => _world.GetEntity(Foo));
        }

        [Fact]
        public void RequestComponent_DestroyedEntity()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            _world.DestroyEntity(Foo);
            Assert.Throws<InvalidOperationException>(() => _world.GetComponent<TestComponent>(default));
        }

        [Fact]
        public void RequestNonExistentComponent()
        {
            Entity entity = _world.CreateEntity(Foo);
            Assert.Throws<InvalidOperationException>(() => _world.GetComponent<TestComponent>(entity));
        }

        [Fact]
        public void RequestComponent_NonExistentEntity()
        {
            Assert.Throws<InvalidOperationException>(() => _world.GetComponent<TestComponent>(default));
        }

        [Fact]
        public void RequestRemovedComponent()
        {
            Entity entity = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(entity);
            _world.RemoveComponent<TestComponent>(entity);
            Assert.Throws<InvalidOperationException>(() => _world.GetComponent<TestComponent>(entity));
        }

        [Fact]
        public void AddSameComponentTypeTwice()
        {
            Entity entity = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(entity);
            Assert.Throws<InvalidOperationException>(() => _world.AddComponent<TestComponent>(entity));
        }

        [Fact]
        public void RemoveNonExistentComponent()
        {
            Entity entity = _world.CreateEntity(Foo);
            Assert.Throws<InvalidOperationException>(() => _world.RemoveComponent<TestComponent>(entity));
        }

        [Fact]
        public void RemoveComponent_NonExistentEntity()
        {
            Assert.Throws<InvalidOperationException>(() => _world.RemoveComponent<TestComponent>(default));
        }

        [Fact]
        public void ReplaceComponent()
        {
            Entity foo = _world.CreateEntity(Foo);
            var original = new TestComponent { Value = 42 };
            _world.AddComponent(foo, ref original);
            _world.RemoveComponent<TestComponent>(foo);

            var @new = new TestComponent { Value = 69 };
            _world.AddComponent(foo, ref @new);

            Assert.Equal(@new, _world.GetComponent<TestComponent>(foo));
        }

        [Fact]
        public void RemoveComponentTwice()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            _world.RemoveComponent<TestComponent>(foo);
            Assert.Throws<InvalidOperationException>(() => _world.RemoveComponent<TestComponent>(foo));
        }

        [Fact]
        public void TestComponentCountLimit()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            _world.AddComponent<TestComponent2>(foo);
            _world.AddComponent<TestComponent3>(foo);
            _world.AddComponent<TestComponent4>(foo);
            _world.AddComponent<TestComponent5>(foo);
            _world.AddComponent<TestComponent6>(foo);
            _world.AddComponent<TestComponent7>(foo);
            _world.AddComponent<TestComponent8>(foo);
            _world.AddComponent<TestComponent9>(foo);
            _world.AddComponent<TestComponent10>(foo);
            _world.AddComponent<TestComponent11>(foo);
            _world.AddComponent<TestComponent12>(foo);
            _world.AddComponent<TestComponent13>(foo);
            _world.AddComponent<TestComponent14>(foo);
            _world.AddComponent<TestComponent15>(foo);
            _world.AddComponent<TestComponent16>(foo);
            _world.AddComponent<TestComponent17>(foo);
            _world.AddComponent<TestComponent18>(foo);
            _world.AddComponent<TestComponent19>(foo);
            _world.AddComponent<TestComponent20>(foo);
            _world.AddComponent<TestComponent21>(foo);
            _world.AddComponent<TestComponent22>(foo);
            _world.AddComponent<TestComponent23>(foo);
            _world.AddComponent<TestComponent24>(foo);
            _world.AddComponent<TestComponent25>(foo);
            _world.AddComponent<TestComponent26>(foo);
            _world.AddComponent<TestComponent27>(foo);
            _world.AddComponent<TestComponent28>(foo);
            _world.AddComponent<TestComponent29>(foo);
            _world.AddComponent<TestComponent30>(foo);
            _world.AddComponent<TestComponent31>(foo);
            _world.AddComponent<TestComponent32>(foo);

            Assert.Throws<InvalidOperationException>(() => _world.AddComponent<TestComponent33>(foo));
        }

        [Fact]
        public void CreateEntityRaisesEvent()
        {
            bool raised = false;
            _world.EntityCreated += name =>
            {
                Assert.Equal(Foo, name);
                raised = true;
            };

            _world.CreateEntity(Foo);
            Assert.True(raised);
        }

        [Fact]
        public void DestroyEntityRaisesEvent()
        {
            _world.CreateEntity(Foo);

            bool fired = false;
            _world.EntityDestroyed += name =>
            {
                Assert.Equal(Foo, name);
                fired = true;
            };

            _world.DestroyEntity(Foo);
            Assert.True(fired);
        }

        [Fact]
        public void AddComponentRaisesEvent()
        {
            Entity foo = _world.CreateEntity(Foo);

            object scBox = null;
            _world.EntityStructureChanged += sc => scBox = sc;
            _world.AddComponent<TestComponent>(foo);

            var change = (StructuralChange)scBox;
            Assert.Equal(StructuralChangeKind.ComponentAdded, change.Kind);
            Assert.Equal(typeof(TestComponent), change.ComponentType);
            Assert.Equal(foo, change.Entity);
        }

        [Fact]
        public void GetComponentRaisesEvent()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);

            bool fired = false;
            _world.ComponentChanged += (e, t, m) =>
            {
                fired = true;
                Assert.Equal(foo, e);
                Assert.Equal(typeof(TestComponent), t);
            };

            _world.GetComponent<TestComponent>(foo);
            Assert.True(fired);
        }

        [Fact]
        public void RemoveComponentRaisesEvent()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);

            object scBox = null;
            _world.EntityStructureChanged += sc => scBox = sc;
            _world.RemoveComponent<TestComponent>(foo);

            var change = (StructuralChange)scBox;
            Assert.Equal(StructuralChangeKind.ComponentRemoved, change.Kind);
            Assert.Equal(typeof(TestComponent), change.ComponentType);
            Assert.Equal(foo, change.Entity);
        }
    }
}
