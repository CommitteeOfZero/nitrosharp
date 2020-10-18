using System;
using System.Collections.Generic;
using System.Diagnostics;
using NitroSharp.Utilities;

#nullable enable

namespace NitroSharp.NsScript.VM
{
    internal readonly struct ThreadAction
    {
        public enum ActionKind
        {
            Create,
            Terminate,
            Suspend,
            Join,
            Resume
        }

        public readonly NsScriptThread Thread;
        public readonly ActionKind Kind;
        public readonly TimeSpan? Timeout;
        public readonly NsScriptThread? JoinedThread;

        public static ThreadAction Create(NsScriptThread thread)
            => new ThreadAction(thread, ActionKind.Create, null);

        public static ThreadAction Terminate(NsScriptThread thread)
            => new ThreadAction(thread, ActionKind.Terminate, null);

        public static ThreadAction Suspend(NsScriptThread thread, TimeSpan? timeout)
            => new ThreadAction(thread, ActionKind.Suspend, timeout);

        public static ThreadAction Join(NsScriptThread thread, NsScriptThread target)
            => new ThreadAction(thread, ActionKind.Join, null, target);

        public static ThreadAction Resume(NsScriptThread thread)
            => new ThreadAction(thread, ActionKind.Resume, null);

        private ThreadAction(
            NsScriptThread thread,
            ActionKind kind,
            TimeSpan? timeout,
            NsScriptThread? joinedThread = null)
        {
            Thread = thread;
            Kind = kind;
            Timeout = timeout;
            JoinedThread = joinedThread;
        }
    }

    public sealed class NsScriptProcess
    {
        private ArrayBuilder<NsScriptThread> _threads;
        private ArrayBuilder<uint> _newThreads;
        private ArrayBuilder<uint> _terminatedThreads;
        private readonly Stopwatch _clock;

        internal readonly Queue<ThreadAction> PendingThreadActions;

        public NsScriptProcess(NsScriptVM vm, NsScriptThread mainThread)
        {
            VM = vm;
            MainThread = mainThread;
            _threads = new ArrayBuilder<NsScriptThread>(16);
            PendingThreadActions = new Queue<ThreadAction>();
            _newThreads = new ArrayBuilder<uint>(8);
            _terminatedThreads = new ArrayBuilder<uint>(8);
            _clock = Stopwatch.StartNew();
            AttachThread(mainThread);
        }

        public NsScriptVM VM { get; }
        public NsScriptThread? MainThread { get; internal set; }
        public NsScriptThread? CurrentThread { get; internal set; }

        internal ReadOnlySpan<NsScriptThread> Threads
            => IsRunning ? _threads.AsReadonlySpan() : default;

        internal ReadOnlySpan<uint> NewThreads => _newThreads.AsReadonlySpan();
        internal ReadOnlySpan<uint> TerminatedThreads => _terminatedThreads.AsReadonlySpan();

        public bool IsRunning => _clock.IsRunning;
        public bool IsTerminated => _threads.Count == 0 && PendingThreadActions.Count == 0;

        public void Suspend()
        {
            _clock.Stop();
        }

        public void Resume()
        {
            _clock.Start();
        }

        public void Terminate()
        {
            foreach (NsScriptThread thread in _threads.AsSpan())
            {
                CommitTerminateThread(thread);
            }

            _clock.Stop();
            _threads.Clear();
            _newThreads.Clear();
        }

        internal void Tick()
        {
            if (!IsRunning) { return; }
            long? time = null;
            _newThreads.Clear();
            _terminatedThreads.Clear();
            foreach (NsScriptThread thread in _threads.AsSpan())
            {
                if (thread.SuspensionTime != null && thread.SleepTimeout != null)
                {
                    time ??= _clock.ElapsedTicks;
                    long delta = time.Value - thread.SuspensionTime.Value;
                    if (delta >= thread.SleepTimeout)
                    {
                        CommitResumeThread(thread);
                    }
                }
            }
        }

        internal void AttachThread(NsScriptThread thread)
        {
            if (!IsRunning) { return; }
            thread.Process = this;
            MainThread ??= thread;
            PendingThreadActions.Enqueue(ThreadAction.Create(thread));
        }

        internal void ProcessPendingThreadActions()
        {
            if (!IsRunning) { return; }
            while (PendingThreadActions.TryDequeue(out ThreadAction action))
            {
                NsScriptThread thread = action.Thread;
                switch (action.Kind)
                {
                    case ThreadAction.ActionKind.Create:
                        _threads.Add(thread);
                        _newThreads.Add(thread.Id);
                        break;
                    case ThreadAction.ActionKind.Terminate:
                        CommitTerminateThread(thread);
                        break;
                    case ThreadAction.ActionKind.Suspend:
                        CommitSuspendThread(thread, action.Timeout);
                        break;
                    case ThreadAction.ActionKind.Join:
                        Debug.Assert(action.JoinedThread is object);
                        CommitJoin(thread, action.JoinedThread);
                        break;
                    case ThreadAction.ActionKind.Resume:
                        CommitResumeThread(thread);
                        break;
                }
            }
        }

        internal void CommitSuspendThread(NsScriptThread thread, TimeSpan? timeoutOpt)
        {
            if (!IsRunning) { return; }
            thread.SuspensionTime = TicksFromTimeSpan(_clock.Elapsed);
            if (timeoutOpt is TimeSpan timeout)
            {
                thread.SleepTimeout = TicksFromTimeSpan(timeout);
            }
        }

        internal void CommitJoin(NsScriptThread thread, NsScriptThread target)
        {
            if (!IsRunning) { return; }
            thread.SuspensionTime = TicksFromTimeSpan(_clock.Elapsed);
            target.WaitingThread = thread;
        }

        internal void CommitResumeThread(NsScriptThread thread)
        {
            if (!IsRunning) { return; }
            thread.SleepTimeout = null;
            thread.SuspensionTime = null;
        }

        internal void CommitTerminateThread(NsScriptThread thread)
        {
            if (!IsRunning) { return; }
            thread.CallFrameStack.Clear();
            thread.EvalStack.Clear();
            _threads.Remove(thread);
            _terminatedThreads.Add(thread.Id);
        }

        private static long TicksFromTimeSpan(TimeSpan timespan)
            => (long)(timespan.TotalSeconds * Stopwatch.Frequency);
    }
}
