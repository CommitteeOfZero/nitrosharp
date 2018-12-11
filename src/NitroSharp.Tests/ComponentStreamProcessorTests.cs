using System;
using System.Collections.Generic;
using NitroSharp.EntitySystem;
using Xunit;

namespace NitroSharp.Tests
{
    public class ComponentStreamProcessorTests
    {
        private class SimpleComponentProcessor : ComponentProcessor<TestComponent>
        {
            public override void ProcessComponentStream(Span<TestComponent> components, ReadOnlySpan<Entity> entities)
            {
            }
        }

        private class NonParallelStreamProcessor : NonParallelComponentStreamProcessor<TestComponent, TestComponent2>
        {
            public override void ProcessNonParallelStreams(Span<TestComponent> streamX, Span<TestComponent2> streamY, HashSet<Entity> entities)
            {
            }
        }

        private class UnregisteredProcessor : ComponentProcessor<TestComponent>
        {
            public override void ProcessComponentStream(Span<TestComponent> components, ReadOnlySpan<Entity> entities)
            {
            }
        }

        private const string Foo = "foo";

        private readonly GameWorld _world = new GameWorld(GameWorldConfiguration.Default);
        private readonly SystemRegistry _registry;
        private readonly SimpleComponentProcessor _simpleProcessor;
        private readonly NonParallelStreamProcessor _nonParallelStreamProcessor;

        public ComponentStreamProcessorTests()
        {
            _registry = new SystemRegistry(_world);
            _simpleProcessor = new SimpleComponentProcessor();
            _nonParallelStreamProcessor = new NonParallelStreamProcessor();
            _registry.Register(_simpleProcessor);
            _registry.Register(_nonParallelStreamProcessor);
        }

        [Fact]
        public void UpdateUnregisteredProcessor()
        {
            var system = new UnregisteredProcessor();
            Assert.Throws<InvalidOperationException>(() => system.Update(0));
        }

        [Fact]
        public void RegisterProcessorTwice()
        {
            Assert.Throws<InvalidOperationException>(() => _registry.Register(_simpleProcessor));
        }

        [Fact]
        public void ProcessorDoesNotTrackEmptyEntity()
        {
            Entity empty = _world.CreateEntity(Foo);
            Assert.False(_simpleProcessor.TracksEntity(empty));
        }

        [Fact]
        public void Processor_TracksEntity_Rejects_InvalidHandle()
        {
            Assert.Throws<InvalidOperationException>(() => _simpleProcessor.TracksEntity(default));
        }

        [Fact]
        public void ProcessorTracksTestEntity()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            Assert.True(_simpleProcessor.TracksEntity(foo));
        }

        [Fact]
        public void ProcessorStopsTrackingEntity_AfterComponentRemoval()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            _world.RemoveComponent<TestComponent>(foo);
            Assert.False(_simpleProcessor.TracksEntity(foo));
        }

        [Fact]
        public void ProcessorStopsTrackingDestroyedEntity()
        {
            Entity foo = _world.CreateEntity(Foo);
            _world.AddComponent<TestComponent>(foo);
            _world.DestroyEntity(Foo);
            Assert.False(_simpleProcessor.TracksEntity(foo));
        }

        [Fact]
        public void NonParallelStream_Processor_Checks_Entity_Mask_Properly()
        {
            var hasOnlyType1 = _world.CreateEntity("Entity that only has a TestComponent");
            _world.AddComponent<TestComponent>(hasOnlyType1);
            var hasOnlyType2 = _world.CreateEntity("Entity that only has a TestComponentTwo");
            _world.AddComponent<TestComponent2>(hasOnlyType2);
            var hasBoth = _world.CreateEntity("Entity that has both components");
            _world.AddComponent<TestComponent>(hasBoth);
            _world.AddComponent<TestComponent2>(hasBoth);

            _nonParallelStreamProcessor.Update(0);
            Assert.False(_nonParallelStreamProcessor.TracksEntity(hasOnlyType1));
            Assert.False(_nonParallelStreamProcessor.TracksEntity(hasOnlyType2));
            Assert.True(_nonParallelStreamProcessor.TracksEntity(hasBoth));
        }
    }
}
