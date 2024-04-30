using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace NitroSharp.Media.NullAudio
{
    internal sealed class NullAudioSource : AudioSource
    {
        private PipeReader? _audioData;
        private CancellationTokenSource? _cts;
        private Task? _consumeTask;

        public override bool IsPlaying => false;
        public override double SecondsElapsed => 0;
        public override float Volume { get; set; }
        public override ReadOnlySpan<short> GetCurrentBuffer() => default;

        public override void Play(PipeReader audioData)
        {
            _ = PlayAsync(audioData);
        }

        public override void Pause()
        {
        }

        public override void Resume()
        {
        }

        public override void Stop()
        {
            _ = StopAsync();
        }

        public override void FlushBuffers()
        {
        }

        private async Task PlayAsync(PipeReader audioData)
        {
            await StopAsync();
            _audioData = audioData;
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoop(_audioData));
        }

        private async Task ConsumeLoop(PipeReader audioData)
        {
            Debug.Assert(_cts is not null);
            SpinWait spinner = new();
            while (!_cts.IsCancellationRequested)
            {
                spinner.SpinOnce();
                ReadResult readResult = await audioData.ReadAsync();
                audioData.AdvanceTo(readResult.Buffer.End);
            }
        }

        private async Task StopAsync()
        {
            if (_audioData is not null && _cts is not null)
            {
                _cts.Cancel();
                _audioData = null;
                if (_consumeTask is not null)
                {
                    await _consumeTask;
                    _consumeTask = null;
                }
            }
        }

        public override async ValueTask DisposeAsync()
        {
            if (_consumeTask is not null)
            {
                await StopAsync();
            }
        }
    }
}
