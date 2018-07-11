// A customized version of https://github.com/dotnet/corefx/blob/master/src/System.Threading.Channels/src/System/Threading/Channels/AsyncOperation.cs
// - Has limited support for cancellation.
// - No support for custom TaskSchedulers.
// - Does not allocate Tasks when asynchronously invoking continuations.
// Modifications made by: @SomeAnonDev.
// The original file is licensed to the .NET Foundation under the MIT license.
// See https://github.com/dotnet/corefx/blob/master/LICENSE.TXT for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace NitroSharp.Media.Decoding
{
    internal class AsyncOperation<TResult> : IValueTaskSource, IValueTaskSource<TResult>
    {
        private static readonly Action<object> s_availableSentinel = (_) => { };
        private static readonly Action<object> s_completedSentinel = (_) => { };

        private readonly bool _runContinuationsAsynchronously;

        private TResult _result;
        private ExceptionDispatchInfo _error;

        // s_completedSentinel if the operation has already completed.
        // s_availableSentinel if it is available for use.
        // null if the operation is pending.
        // another callback if the operation has had a callback hooked up with OnCompleted.
        private Action<object> _continuation;
        private object _continuationState;

        // IValueTaskSource operations are only valid if the provided token matches this value,
        // which is incremented once GetResult is called to avoid multiple awaits on the same instance.
        private short _currentId;

        private readonly ConcurrentQueue<(Action<object> action, object state)> _threadPoolQueue;
        private readonly WaitCallback _threadPoolCallback;
        private readonly Action<object> _complete;

        public AsyncOperation(bool runContinuationsAsynchronously)
        {
            _continuation = s_availableSentinel;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            _threadPoolQueue = new ConcurrentQueue<(Action<object> action, object state)>();
            _threadPoolCallback = new WaitCallback(ThreadPoolCallback);
            _complete = new Action<object>(SetCompletionAndInvokeContinuation);
        }

        public ValueTask ValueTask => new ValueTask(this, _currentId);

        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (_currentId == token)
            {
                return
                    !IsCompleted ? ValueTaskSourceStatus.Pending :
                    _error == null ? ValueTaskSourceStatus.Succeeded :
                    _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                    ValueTaskSourceStatus.Faulted;
            }

            ThrowIncorrectCurrentIdException();
            return default;
        }

        internal bool IsCompleted => ReferenceEquals(_continuation, s_completedSentinel);

        public TResult GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo error = _error;
            TResult result = _result;
            _currentId++;

            Volatile.Write(ref _continuation, s_availableSentinel);

            error?.Throw();
            return result;
        }

        void IValueTaskSource.GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo error = _error;
            _currentId++;

            Volatile.Write(ref _continuation, s_availableSentinel);

            error?.Throw();
        }

        public bool TryOwnAndReset()
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _continuation, null, s_availableSentinel), s_availableSentinel))
            {
                _continuationState = null;
                _result = default;
                _error = null;
                return true;
            }

            return false;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            // We need to store the state before the CompareExchange, so that if it completes immediately
            // after the CompareExchange, it'll find the state already stored.  If someone misuses this
            // and schedules multiple continuations erroneously, we could end up using the wrong state.
            // Make a best-effort attempt to catch such misuse.
            if (_continuationState != null)
            {
                ThrowMultipleContinuations();
            }
            _continuationState = state;

            // Try to set the provided continuation into _continuation.  If this succeeds, that means the operation
            // has not yet completed, and the completer will be responsible for invoking the callback.  If this fails,
            // that means the operation has already completed, and we must invoke the callback, but because we're still
            // inside the awaiter's OnCompleted method and we want to avoid possible stack dives, we must invoke
            // the continuation asynchronously rather than synchronously.
            Action<object> prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (prevContinuation != null)
            {
                // If the set failed because there's already a delegate in _continuation, but that delegate is
                // something other than s_completedSentinel, something went wrong, which should only happen if
                // the instance was erroneously used, likely to hook up multiple continuations.
                Debug.Assert(IsCompleted, $"Expected IsCompleted");
                if (!ReferenceEquals(prevContinuation, s_completedSentinel))
                {
                    Debug.Assert(prevContinuation != s_availableSentinel, "Continuation was the available sentinel.");
                    ThrowMultipleContinuations();
                }

                QueueThreadPoolWorkItem(continuation, state);
            }
        }

        private void QueueThreadPoolWorkItem(Action<object> continuation, object state)
        {
            _threadPoolQueue.Enqueue((continuation, state));
            ThreadPool.QueueUserWorkItem(_threadPoolCallback, null);
        }

        private void ThreadPoolCallback(object o)
        {
            if (_threadPoolQueue.TryDequeue(out (Action<object> action, object state) workItem))
            {
                workItem.action(workItem.state);
            }
        }

        public void SetResult(TResult item)
        {
            _result = item;
            SignalCompletion();
        }

        public void SetException(Exception exception)
        {
            _error = ExceptionDispatchInfo.Capture(exception);
            SignalCompletion();
        }

        public void SetCanceled(CancellationToken cancellationToken = default)
        {
            _error = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
            SignalCompletion();
        }

        private void SignalCompletion()
        {
            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null) != null)
            {
                SignalCompletionCore();
            }
        }

        private void SignalCompletionCore()
        {
            Debug.Assert(_continuation != s_completedSentinel, $"The continuation was the completion sentinel.");
            Debug.Assert(_continuation != s_availableSentinel, $"The continuation was the available sentinel.");

            if (_runContinuationsAsynchronously)
            {
                QueueThreadPoolWorkItem(_complete, null);
                return;
            }

            SetCompletionAndInvokeContinuation();
        }

        private void SetCompletionAndInvokeContinuation(object o = null)
        {
            Action<object> c = _continuation;
            _continuation = s_completedSentinel;
            c(_continuationState);
        }

        protected static void ThrowIncompleteOperationException() =>
            throw new InvalidOperationException("The asynchronous operation has not completed.");

        protected static void ThrowMultipleContinuations() =>
            throw new InvalidOperationException("Another continuation was already registered.");

        protected static void ThrowIncorrectCurrentIdException() =>
            throw new InvalidOperationException("The result of the operation was already consumed and may not be used again.");
    }
}
