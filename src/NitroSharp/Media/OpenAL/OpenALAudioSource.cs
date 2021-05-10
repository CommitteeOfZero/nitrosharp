using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Pipelines;
using System.Buffers;
using System.Threading.Tasks;
using OpenAL;

namespace NitroSharp.Media.OpenAL
{
    internal class OpenALAudioSource : AudioSource
    {
        private readonly int _bufferSize;
        private readonly int _bufferPoolSize;
        private readonly uint _source;
        private readonly uint[] _buffers;
        private int _nextBuffer;
        private CancellationTokenSource _cts;
        private OpenALAudioDevice _audioDevice;
        private Task? _consumeTask;
        private int _buffersLeft;
        private IntPtr[] _bufferData;
        private IntPtr _buffer;
        private int[] _actualBufferSizes;

        public OpenALAudioSource(OpenALAudioDevice audioDevice, int bufferSize, int bufferCount)
        {
            _audioDevice = audioDevice;
            _bufferSize = bufferSize;
            _bufferPoolSize = bufferCount;
            AL10.alGenSources(1, out _source);
            _buffers = new uint[bufferCount];
            _bufferData = new IntPtr[bufferCount];
            AL10.alGenBuffers(bufferCount, _buffers);
            _buffersLeft = bufferCount;
            _buffer = Marshal.AllocHGlobal(_bufferSize * _bufferPoolSize);
            for (int i = 0; i < _bufferPoolSize; i++)
            {
                _bufferData[i] = IntPtr.Add(_buffer, i * _bufferSize);
            }
            _actualBufferSizes = new int[bufferCount];
        }

        public override bool IsPlaying
        {
            get
            {
                AL10.alGetSourcei(_source, AL10.AL_SOURCE_STATE, out int state);
                return state == AL10.AL_PLAYING;
            }
        }

        public override double SecondsElapsed { get; }
        public override float Volume
        {
            get
            {
                AL10.alGetSourcef(_source, AL10.AL_GAIN, out float gain);
                return gain;
            }
            set => AL10.alSourcef(_source, AL10.AL_GAIN, value);
        }

        public override void Play(PipeReader audioData)
        {
            _ = PlayAsync(audioData);
        }

        private async Task PlayAsync(PipeReader audioData)
        {
            await StopAsync();
            AL10.alSourcePlay(_source);
            _cts = new CancellationTokenSource();
            _consumeTask = Task.Run(() => ConsumeLoop(audioData));
        }

        private async Task ConsumeLoop(PipeReader audioData)
        {
            while (!_cts.IsCancellationRequested)
            {
                ReadResult readResult = await audioData.ReadAsync();
                BufferData(audioData, readResult);
                // TODO
                /* await Task.Delay(100); */
            }
        }

        private unsafe void BufferData(PipeReader pipeReader, in ReadResult readResult)
        {
            ReadOnlySequence<byte> sequence = readResult.Buffer;
            AL10.alGetSourcei(_source, AL10.AL_BUFFERS_PROCESSED, out int buffersProcessed);
            uint[] processed = new uint[buffersProcessed];

            AL10.alSourceUnqueueBuffers(_source, buffersProcessed, processed);
            _buffersLeft += buffersProcessed;

            bool whatever = !sequence.IsEmpty &&  _buffersLeft > 0;
            while (!sequence.IsEmpty &&  _buffersLeft > 0)
            {
                int size = Math.Min((int)sequence.Length, _bufferSize);
                ReadOnlySequence<byte> src = sequence.Slice(0, size);
                sequence = sequence.Slice(size, sequence.Length - size);
                var dst = new Span<byte>(_bufferData[_nextBuffer].ToPointer(), size);
                src.CopyTo(dst);

                _actualBufferSizes[_nextBuffer] = size;
                AudioParameters param = _audioDevice.AudioParameters;
                AL10.alBufferData(_buffers[_nextBuffer], AL10.AL_FORMAT_STEREO16, _bufferData[_nextBuffer], size, (int) param.SampleRate);
                int error = AL10.alGetError();
                if (error != 0)
                {
                }

                AL10.alSourceQueueBuffers(_source, 1, ref _buffers[_nextBuffer]);
                _nextBuffer = (_nextBuffer + 1) % _buffers.Length;
                _buffersLeft--;
            }
            // TODO
            if (whatever)
            {
                if (this.IsPlaying == false)
                {
                    AL10.alSourcePlay(_source);
                }
            }

            pipeReader.AdvanceTo(sequence.Start);
        }

        public override void Pause()
        {
            AL10.alSourcePause(_source);
        }

        public override void Resume()
        {
            AL10.alSourcePlay(_source);
        }

        public override void Stop()
        {
            _ = StopAsync();
        }

        private async Task StopAsync()
        {
            if (_cts  != null)
            {
                _cts.Cancel();
            }
            if (_consumeTask != null)
            {
                await _consumeTask;
            }
            AL10.alSourcePause(_source);
            AL10.alSourceStop(_source);
        }

        // TODO
        public override void FlushBuffers()
        {
        }

        public override unsafe ReadOnlySpan<short> GetCurrentBuffer()
        {
            int currentBuffer = (_nextBuffer - 1 + _buffersLeft) % _bufferPoolSize;
            if (currentBuffer < 0) return default;
            int size = _actualBufferSizes[currentBuffer];
            return new ReadOnlySpan<short>(_bufferData[currentBuffer].ToPointer(), size / 2);
        }

        public override ValueTask DisposeAsync()
        {
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;
            }
            return new ValueTask(null);
        }
    }
}
