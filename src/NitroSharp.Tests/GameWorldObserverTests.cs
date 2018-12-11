using System;
using NitroSharp.EntitySystem;
using Xunit;

namespace NitroSharp.Tests
{
    public class GameWorldObserverTests
    {
        private const string Foo = "foo";

        [Fact]
        public void ReplayEmptyCommandList()
        {
            Test(w => { });
        }

        [Fact]
        public void ReplayCreateEntity()
        {
            Test(w => w.CreateEntity(Foo));
        }

        [Fact]
        public void ReplayAddComponent()
        {
            Test(w =>
            {
                var foo = w.CreateEntity(Foo);
            },
            w =>
            {
                w.AddComponent<TestComponent>(w.GetEntity(Foo));
            });
        }

        [Fact]
        public void ReplayComponentChange()
        {
            Test(w =>
            {
                var foo = w.CreateEntity(Foo);
                w.AddComponent(foo, new TestComponent { Value = 42 });
            },
            w =>
            {
                ref var c = ref w.GetComponent<TestComponent>(w.GetEntity(Foo));
                c = new TestComponent { Value = 69 };
            });
        }

        [Fact]
        public void ReplayRemoveComponent()
        {
            Test(w =>
            {
                var foo = w.CreateEntity(Foo);
                w.AddComponent<TestComponent>(foo);
            },
            w =>
            {
                w.RemoveComponent<TestComponent>(w.GetEntity(Foo));
            });
        }

        [Fact]
        public void Replay_AddComponent_Then_DestroyEntity()
        {
            Test(w =>
            {
                var foo = w.CreateEntity(Foo);
            },
            w =>
            {
                var foo = w.GetEntity(Foo);
                w.AddComponent<TestComponent>(foo);
                w.DestroyEntity(Foo);
            });
        }

        [Fact]
        public void Replay_AddComponent_ModifyComponent_Then_DestroyEntity()
        {
            Test(w =>
            {
                var foo = w.CreateEntity(Foo);
            },
            w =>
            {
                var foo = w.GetEntity(Foo);
                w.AddComponent<TestComponent>(foo);
                w.GetComponent<TestComponent>(foo) = new TestComponent();
                w.DestroyEntity(Foo);
            });
        }

        private void Test(Action<GameWorld> setup, Action<GameWorld> mutate = null)
        {
            var a = new GameWorld(GameWorldConfiguration.Default);
            var cl = new GameWorldObserver(a);
            setup(a);
            var b = new GameWorld(GameWorldConfiguration.Default);
            cl.ReplayChanges(b);
            Assert.True(GameWorld.Debug_AreIdentical(a, b));

            if (mutate != null)
            {
                cl.Reset();
                mutate(a);
                cl.ReplayChanges(b);
                Assert.True(GameWorld.Debug_AreIdentical(a, b));
            }
        }
    }
}
