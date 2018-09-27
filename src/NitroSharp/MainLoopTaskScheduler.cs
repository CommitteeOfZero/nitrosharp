using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NitroSharp
{
    internal sealed class MainLoopTaskScheduler : TaskScheduler
    {
        private readonly int _mainThreadId;
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        public MainLoopTaskScheduler()
        {
            _mainThreadId = Environment.CurrentManagedThreadId;
        }

        public void FlushQueuedTasks()
        {
            Task t;
            while (_tasks.TryTake(out t))
            {
                TryExecuteTask(t);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
            //return Environment.CurrentManagedThreadId == _mainThreadId && TryExecuteTask(task);
        }
    }
}
