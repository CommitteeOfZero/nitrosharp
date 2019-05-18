using System;
using System.Threading.Tasks;
using System.Threading;
using FFmpeg.AutoGen;
using System.Runtime.CompilerServices;

namespace NitroSharp.Media.Decoding
{
    internal sealed partial class MediaProcessingPipeline
    {
        // This here is a manually written async state machine.
        // It can be reset and reused, and it doesn't use Tasks.
        // This allows to avoid megabytes of allocations.
        private sealed class DecodedFrameReceiver
        {
            /* Semantically, this code should be identical to the following:
            private async ValueTask ReceiveFramesFromDecoder()
            {
                var output = _decodedFrames.Writer;
                int error = 0;
                do
                {
                    IntPtr memoryChunk = await _avFramePool.RentChunkAsync();
                    var pooledFrame = new PooledStruct<AVFrame>(memoryChunk, _avFramePool);
                    bool sent = false;
                    try
                    {
                        error = _decodingSession.TryReceiveFrame(ref pooledFrame.AsRef());
                        if (error == 0)
                        {
                            await output.WriteAsync(pooledFrame);
                            sent = true;
                        }
                    }
                    finally
                    {
                        if (!sent)
                        {
                            pooledFrame.Free();
                        }
                    }
                } while (error == 0);
            }
            */

            private readonly struct VoidResult { }

            private readonly MediaProcessingPipeline _parent;
            private readonly AsyncOperation<VoidResult> _asyncOperation;
            private readonly Action _rentAvFrame;
            private readonly Action _onRentCompleted;
            private ValueTask<IntPtr> _rentFrameTask;
            private PooledStruct<AVFrame> _pooledFrame;
            private ValueTask _submitFrameTask;
            private CancellationToken _cancellationToken;
            private bool _async;

            public DecodedFrameReceiver(MediaProcessingPipeline parent)
            {
                _parent = parent;
                _asyncOperation = new AsyncOperation<VoidResult>(runContinuationsAsynchronously: true);
                _rentAvFrame = new Action(RentAvFrame);
                _onRentCompleted = new Action(OnRentCompleted);
            }

            /// <summary>
            /// Receives AVFrames from the decoder and submits them to the frame queue.
            /// Stops once EAGAIN is returned.
            /// </summary>
            public ValueTask ReceiveFramesFromDecoder(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }

                _cancellationToken = cancellationToken;
                _async = false;

                RentAvFrame();
                if (!_async) { return default; }
                return _asyncOperation.ValueTask;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void TakeAsyncPath()
            {
                _async = true;
                _asyncOperation.TryOwnAndReset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CancelIfRequested()
            {
                bool requested = _cancellationToken.IsCancellationRequested;
                if (_async && requested)
                {
                    _asyncOperation.SetCanceled(_cancellationToken);
                }

                return requested;
            }

            private void RentAvFrame()
            {
                if (CancelIfRequested()) { return; }

                _rentFrameTask = _parent._avFramePool.RentChunkAsync();
                if (_rentFrameTask.IsCompleted)
                {
                    OnRentCompleted();
                }
                else
                {
                    TakeAsyncPath();
                    _rentFrameTask.GetAwaiter().OnCompleted(_onRentCompleted);
                }
            }

            private void OnRentCompleted()
            {
                IntPtr memoryChunk = _rentFrameTask.GetAwaiter().GetResult();
                _pooledFrame = new PooledStruct<AVFrame>(memoryChunk, _parent._avFramePool);
                if (!CancelIfRequested())
                {
                    ReceiveDecodedFrame();
                }
                else
                {
                    _pooledFrame.Free();
                }
            }

            private void ReceiveDecodedFrame()
            {
                var output = _parent._decodedFrames.Writer;
                int error = _parent._decodingSession.TryReceiveFrame(ref _pooledFrame.AsRef());
                if (CancelIfRequested())
                {
                    _pooledFrame.Free();
                    return;
                }

                if (error == 0)
                {
                    _submitFrameTask = output.WriteAsync(_pooledFrame);
                    if (_submitFrameTask.IsCompleted)
                    {
                        RentAvFrame();
                    }
                    else
                    {
                        TakeAsyncPath();
                        _submitFrameTask.GetAwaiter().OnCompleted(_rentAvFrame);
                    }
                }
                else
                {
                    _pooledFrame.Free();
                    if (_async)
                    {
                        _asyncOperation.SetResult(default);
                    }
                }
            }
        }
    }
}
