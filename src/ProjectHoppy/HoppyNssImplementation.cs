using System;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using System.Linq;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;

namespace ProjectHoppy
{
    //public class GameObjectManager
    //{
    //    private Dictionary<string, Entity> _objects;

    //    public GameObjectManager()
    //    {
    //        _objects = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
    //    }

    //    public void Add(string name, Entity obj)
    //    {
    //        _objects[name] = obj;
    //    }

    //    public Entity Get(string objectName)
    //    {
    //        _objects.TryGetValue(objectName, out var result);
    //        return result;
    //    }

    //    public IEnumerable<Entity> WildcardQuery(string query)
    //    {
    //        query = query.Replace("*", string.Empty);
    //        return _objects.Where(x => x.Key.ToUpper().StartsWith(query)).Select(x => x.Value);
    //    }
    //}

    public class HoppyNssImplementation : NssBuiltInMethods
    {
        private readonly EntityManager _entities;

        public HoppyNssImplementation(EntityManager entities)
        {
            _entities = entities;
        }

        public override void AddRectangle(string entityName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            _entities.CreateEntity(entityName)
                .WithComponent(new VisualComponent(x.Value, y.Value, width, height, zLevel))
                .WithComponent(new ShapeComponent(ShapeKind.Rectangle, Color.Black));
        }

        //public override void AddTexture(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, string fileOrObjectName)
        //{
        //    Texture2D texture;
        //    try
        //    {
        //        texture = _game.Content.Load<Texture2D>(fileOrObjectName);
        //    }
        //    catch (ContentLoadException)
        //    {
        //        return;
        //    }

        //    var visual = new TextureVisual(texture, x.Value, y.Value, (int)texture.Width, (int)texture.Height, zLevel);
        //    _objects.Add(objectName, visual);
        //    _game.Graphics.AddRenderItem(visual);
        //}

        //public override void FadeIn(string objectName, TimeSpan duration, int opacity, bool wait)
        //{
        //    if (objectName == null)
        //    {
        //        return;
        //    }

        //    var obj = _objects.Get(objectName) as RenderItem;
        //    if (obj != null)
        //    {
        //        obj.FadeIn(duration, opacity / 1000);
        //        if (duration.TotalMilliseconds > 0)
        //        _game.Interact(duration);
        //    }
        //}

        public override void Wait(TimeSpan delay)
        {
            //_game.Interact(delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            //_game.Interact(timeout);
        }

        public override void WaitText(string id, TimeSpan time)
        {
            //_game.Interact(TimeSpan.FromSeconds(10));
        }
    }
}
