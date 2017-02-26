using SciAdvNet.MediaLayer;
using SciAdvNet.MediaLayer.Graphics;
using SciAdvNet.MediaLayer.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectHoppy.Graphics
{
    public class GraphicsSystem : GameSystem
    {
        private readonly SortedList<int, RenderItem> _renderItems;

        public GraphicsSystem(Window window)
        {
            var backend = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GraphicsBackend.DirectX : GraphicsBackend.OpenGL;
            RenderContext = RenderContext.Create(backend, window);

            _renderItems = new SortedList<int, RenderItem>(new DuplicateKeyComparer<int>());
        }

        public RenderContext RenderContext { get; }

        public void AddRenderItem(RenderItem renderItem)
        {
            _renderItems.Add(renderItem.LayerDepth, renderItem);
        }

        public void Render()
        {
            using (var session = RenderContext.NewSession(Color.White))
            {
                foreach (var item in _renderItems.Values.Reverse())
                {
                    item.Render(session);
                }
            }
        }

        public override void Update()
        {
            foreach (var item in _renderItems.Values)
            {
                item.Update();
            }
        }

        public override void Dispose()
        {
            RenderContext.Dispose();
        }

        private class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                int result = Comparer<TKey>.Default.Compare(x, y);
                return result == 0 ? -1 : result;
            }
        }
    }
}
