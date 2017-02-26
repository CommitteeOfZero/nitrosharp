using System;
using ProjectHoppy.Graphics;
using SciAdvNet.MediaLayer;
using SciAdvNet.NSScript.Execution;
using System.Collections.Generic;
using System.Linq;
using SciAdvNet.MediaLayer.Graphics;
using ProjectHoppy.Content;

namespace ProjectHoppy
{
    public class GameObjectManager
    {
        private Dictionary<string, GameObject> _objects;

        public GameObjectManager()
        {
            _objects = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        }

        public void Add(string name, GameObject obj)
        {
            _objects[name] = obj;
        }

        public GameObject Get(string objectName)
        {
            _objects.TryGetValue(objectName, out var result);
            return result;
        }

        public IEnumerable<GameObject> WildcardQuery(string query)
        {
            query = query.Replace("*", string.Empty);
            return _objects.Where(x => x.Key.ToUpper().StartsWith(query)).Select(x => x.Value);
        }
    }

    public class HoppyNssImplementation : NssBuildInMethods
    {
        private readonly Game _game;
        private readonly GameObjectManager _objects;

        public HoppyNssImplementation(Game game)
        {
            _game = game;
            _objects = new GameObjectManager();
        }

        public override void AddRectangle(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, int width, int height, NssColor color)
        {
            var rectangle = new RectangleVisual(x.Value, y.Value, width, height, zLevel, new Color(color.R, color.G, color.B));
            _objects.Add(objectName, rectangle);
            _game.Graphics.AddRenderItem(rectangle);
        }

        public override void AddTexture(string objectName, int zLevel, NssCoordinate x, NssCoordinate y, string fileOrObjectName)
        {
            Texture2D texture;
            try
            {
                texture = _game.Content.Load<Texture2D>(fileOrObjectName);
            }
            catch (ContentLoadException)
            {
                return;
            }

            var visual = new TextureVisual(texture, x.Value, y.Value, (int)texture.Width, (int)texture.Height, zLevel);
            _objects.Add(objectName, visual);
            _game.Graphics.AddRenderItem(visual);
        }

        public override void FadeIn(string objectName, TimeSpan duration, int opacity, bool wait)
        {
            if (objectName == null)
            {
                return;
            }

            var obj = _objects.Get(objectName) as RenderItem;
            if (obj != null)
            {
                obj.FadeIn(duration, opacity / 1000);
                if (duration.TotalMilliseconds > 0)
                _game.Interact(duration);
            }
        }

        public override void Wait(TimeSpan delay)
        {
            _game.Interact(delay);
        }

        public override void WaitForInput(TimeSpan timeout)
        {
            _game.Interact(timeout);
        }

        public override void WaitText(string id, TimeSpan time)
        {
            _game.Interact(TimeSpan.FromSeconds(10));
        }
    }
}
