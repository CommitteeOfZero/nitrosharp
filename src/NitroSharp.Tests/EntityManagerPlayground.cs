using System;
using Xunit;
using NitroSharp.Experimental;
using System.Linq;
using NitroSharp.NsScript;

namespace NitroSharp.Tests
{
    public class EntityManagerPlayground
    {
        private EntityManager _world = new EntityManager();

        //[Fact]
        //public void ViolateTypeSafety()
        //{
        //    NewEntity e = _world.CreateEntity(_storage);
        //    Assert.Throws<InvalidOperationException>(() => _world.Get<uint>(e));
        //}

        [Fact]
        public void InsertionWorks()
        {
            const uint n = 1024;
            for (uint i = 0; i < n; i++)
            {
                var name = new EntityName(i.ToString());
                Entity e = _world.CreateSprite(name);
                Assert.Equal(i, e.Index);
                Assert.Equal(e, _world.GetEntity(name));
            }
            Assert.Equal(n, (uint)_world.Sprites.Active.Length);
        }

        [Fact]
        public void DeactivatingEntitiesWorks()
        {
            var name = new EntityName("foo");
            Entity e = _world.CreateSprite(name);
            _world.CreateSprite(new EntityName("foo2"));
            _world.CreateSprite(new EntityName("foo3"));
            Assert.Equal(3, _world.Sprites.Active.Length);
            _world.DisableEntity(e);
            Assert.Equal(2, _world.Sprites.Active.Length);
            Assert.Equal(1, _world.Sprites.Inactive.Length);

            _world.EnableEntity(e);
            Assert.Equal(3, _world.Sprites.Active.Length);
            Assert.Equal(0, _world.Sprites.Inactive.Length);
        }

        //[Fact]
        //public void RemovingWorksCorrectly()
        //{
        //    const int n = 1024;
        //    for (int i = 0; i < n; i++)
        //    {
        //        _world.CreateEntity(_storage);
        //        _storage.All[i] = i;
        //    }

        //    for (int i = 0; i < n / 2; i++)
        //    {
        //        int lastElem = _storage.All[^1];
        //        NewEntity e = _storage.Entities[i];
        //        Assert.True(_world.Exists(e));
        //        _world.DestroyEntity(e, _storage);
        //        Assert.False(_world.Exists(e));
        //        Assert.Equal(lastElem, _storage.All[i]);
        //        Assert.Equal(n - i - 1, (int)_storage.Count);
        //    }

        //    Assert.Equal(Enumerable.Range(n / 2, n / 2).Reverse(), _storage.All.ToArray());
        //    for (int i = 0; i < n / 2; i++)
        //    {
        //        NewEntity e = _storage.Entities[i];
        //        Assert.Equal(i, (int)_world.GetIndexInStorage(e));
        //    }
        //}

        //[Fact]
        //public void RemoveAllThenCreate()
        //{
        //    const int n = 1024;
        //    for (int i = 0; i < n; i++)
        //    {
        //        _world.CreateEntity(_storage);
        //        _storage.All[i] = i;
        //    }

        //    for (int i = 0; i < n; i++)
        //    {
        //        NewEntity e = _storage.Entities[0];
        //        _world.DestroyEntity(e, _storage);
        //    }

        //    Assert.Equal(0u, _storage.Count);

        //    for (int i = 0; i < n; i++)
        //    {
        //        NewEntity e = _world.CreateEntity(_storage);
        //        Assert.Equal(2u, e.Version);
        //        Assert.Equal(i, (int)_world.GetIndexInStorage(e));
        //    }
        //}

        //[Fact]
        //public void RemoveLastEntity()
        //{
        //    const int n = 1024;
        //    for (int i = 0; i < n; i++)
        //    {
        //        _world.CreateEntity(_storage);
        //        _storage.All[i] = i;
        //    }

        //    NewEntity e = _storage.Entities[^1];
        //    Assert.True(_world.Exists(e));
        //    _world.DestroyEntity(_storage.Entities[^1], _storage);
        //    Assert.False(_world.Exists(e));
        //    Assert.Equal(n - 1, (int)_storage.Count);
        //}
    }
}
