using System;
using SciAdvNet.MediaLayer.Graphics;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace ProjectHoppy.Content
{
    public class ConcurrentContentManager : ContentManager
    {
        private readonly ConcurrentDictionary<string, object> _loadedItems;
        private readonly BufferBlock<string> _workItems;

        public ConcurrentContentManager()
        {
            _loadedItems = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _workItems = new BufferBlock<string>();

            var executionOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 };
            var load = new ActionBlock<string>(x =>
            {
                _loadedItems[x] = Load(x);
            }, executionOptions);

            _workItems.LinkTo(load, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public void EnqueueWorkItem(string path) => _workItems.Post(path);

        public bool IsLoaded(string path) => _loadedItems.ContainsKey(path);
        public T Get<T>(string path) => (T)_loadedItems[path];
        //{
        //    _loadedItems.TryRemove(path, out var result);
        //    return (T)result;
        //}
    }
}
