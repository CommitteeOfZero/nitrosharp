using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NitroSharp.Foundation
{
    public class MainLoopTaskScheduler : TaskScheduler
    {
        private readonly int _mainThreadId;
        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        public MainLoopTaskScheduler(int mainThreadId)
        {
            _mainThreadId = mainThreadId;
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
            return Environment.CurrentManagedThreadId == _mainThreadId && TryExecuteTask(task);
        }
    }
}
