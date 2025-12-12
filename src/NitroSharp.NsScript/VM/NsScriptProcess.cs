using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NitroSharp.NsScript.VM;

[Persistable]
public readonly partial struct NsScriptProcessDump
{
    internal uint Id { get; init; }
    internal double ClockBaseMs { get; init; }
    internal NsScriptThreadDump[] Threads { get; init; }
    internal uint MainThread { get; init; }
}

//
// public sealed class NsScriptProcess
// {
//     private ArrayBuilder<NsScriptThread> _threads;
//     private ArrayBuilder<uint> _newThreads;
//     private ArrayBuilder<uint> _terminatedThreads;
//     private readonly Stopwatch _clock;
//     private readonly long _clockBase;
//
//     public NsScriptProcess(NsScriptVM vm, uint id, NsScriptThread mainThread)
//     {
//         VM = vm;
//         Id = id;
//         MainThread = mainThread;
//         _threads = new ArrayBuilder<NsScriptThread>(16);
//         _newThreads = new ArrayBuilder<uint>(8);
//         _terminatedThreads = new ArrayBuilder<uint>(8);
//         _clockBase = 0;
//         _clock = Stopwatch.StartNew();
//         AttachThread(mainThread);
//     }
//
//     public NsScriptThread GetThread(uint id)
//         => _threads.UnderlyingArray.First(x => x.Id == id);
//
//     internal NsScriptProcess(NsScriptVM vm, NsScriptProcessDump dump)
//     {
//         VM = vm;
//         Id = dump.Id;
//         _clockBase = (int)Math.Round(Stopwatch.Frequency / 1000.0d * dump.ClockBaseMs);
//
//         _threads = new ArrayBuilder<NsScriptThread>(16);
//         _newThreads = new ArrayBuilder<uint>(8);
//         _terminatedThreads = new ArrayBuilder<uint>(8);
//
//         NsScriptThread[] threads = dump.Threads
//             .Select(x => new NsScriptThread(vm, this, x))
//             .ToArray();
//         _threads.AddRange(threads);
//
//         foreach ((NsScriptThread thread, NsScriptThreadDump threadDump) in threads.Zip(dump.Threads))
//         {
//             if (threadDump.WaitingThread is { } waitingThread)
//             {
//                 thread.WaitingThread = threads.First(x => x.Id == waitingThread);
//             }
//         }
//
//         MainThread = threads.First(x => x.Id == dump.MainThread);
//         _clock = Stopwatch.StartNew();
//     }
//
//     public NsScriptVM VM { get; }
//     public uint Id { get; }
//     public NsScriptThread MainThread { get; }
//     public NsScriptThread? CurrentThread { get; internal set; }
//
//     internal ReadOnlySpan<NsScriptThread> Threads
//         => IsRunning ? _threads.AsReadonlySpan() : default;
//
//     internal ReadOnlySpan<uint> NewThreads => _newThreads.AsReadonlySpan();
//     internal ReadOnlySpan<uint> TerminatedThreads => _terminatedThreads.AsReadonlySpan();
//
//     private long Ticks => _clockBase + _clock.ElapsedTicks;
//
//     public bool IsRunning => _clock.IsRunning;
//     public bool IsTerminated => _threads.Count == 0 && PendingThreadActions.Count == 0;
//
//     public void Suspend()
//     {
//         _clock.Stop();
//     }
//
//     public void Resume()
//     {
//         _clock.Start();
//     }
//
//     public void Terminate()
//     {
//         foreach (NsScriptThread thread in _threads.AsSpan())
//         {
//             CommitTerminateThread(thread);
//         }
//
//         _clock.Stop();
//         _threads.Clear();
//         _newThreads.Clear();
//     }
//
//     internal void Tick()
//     {
//         if (!IsRunning) { return; }
//         long? time = null;
//         _newThreads.Clear();
//         _terminatedThreads.Clear();
//         foreach (NsScriptThread thread in _threads.AsSpan())
//         {
//             if (thread is { SuspensionTime: { }, SleepTimeout: { } })
//             {
//                 time ??= Ticks;
//                 long delta = time.Value - thread.SuspensionTime.Value;
//                 if (delta >= thread.SleepTimeout)
//                 {
//                     CommitResumeThread(thread);
//                 }
//             }
//         }
//     }
//
//     // internal void CommitSuspendThread(NsScriptThread thread, TimeSpan? timeoutOpt)
//     // {
//     //     if (!IsRunning) { return; }
//     //     thread.SuspensionTime = Ticks;
//     //     if (timeoutOpt is { } timeout)
//     //     {
//     //         thread.SleepTimeout = (long)Math.Round(timeout.TotalSeconds * Stopwatch.Frequency);
//     //     }
//     // }
//     //
//     // private void CommitJoin(ref NsScriptThreadState thread, ref NsScriptThreadState target)
//     // {
//     //     if (!IsRunning) { return; }
//     //     thread.SuspensionTime = Ticks;
//     //     target.WaitingThread = thread;
//     // }
//     //
//     // private void CommitResumeThread(NsScriptThread thread)
//     // {
//     //     if (!IsRunning) { return; }
//     //     thread.SleepTimeout = null;
//     //     thread.SuspensionTime = null;
//     // }
//     //
//     // private void CommitTerminateThread(NsScriptThread thread)
//     // {
//     //     thread.CallFrameStack.Clear();
//     //     thread.EvalStack.Clear();
//     //     _threads.Remove(thread);
//     //     _terminatedThreads.Add(thread.Id);
//     // }
//
//     public NsScriptProcessDump Dump()
//     {
//         Debug.Assert(PendingThreadActions.Count == 0);
//         return new NsScriptProcessDump
//         {
//             Id = Id,
//             ClockBaseMs = Ticks / (double)Stopwatch.Frequency * 1000.0d,
//             MainThread = MainThread.Id,
//             Threads = _threads.ToArray().Select(x => x.Dump()).ToArray()
//         };
//     }
// }
