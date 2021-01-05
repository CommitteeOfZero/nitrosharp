using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace NitroSharp.Media.XAudio2
{
    public sealed class XAudio2AudioSource : AudioSource
    {
        private readonly XAudio2AudioDevice _device;
        private readonly SourceVoice _sourceVoice;
        private readonly ConcurrentQueue<IntPtr> _bufferQueue = new();
        private readonly AudioBuffer _xaudioBuffer = new();

        private volatile IntPtr _currentBufferPointer;
        private long _lastNbSamplesPlayed;

        public XAudio2AudioSource(XAudio2AudioDevice audioDevice)
        {
            _device = audioDevice;
            AudioParameters parameters = audioDevice.AudioParameters;
            var waveFormat = new WaveFormat((int)parameters.SampleRate, AudioDevice.BitDepth, parameters.ChannelCount);
            _sourceVoice = new SourceVoice(audioDevice.Device, waveFormat, VoiceFlags.NoSampleRateConversion);
            _sourceVoice.BufferStart += OnBufferStart;
            _sourceVoice.BufferEnd += OnBufferEnd;
        }

        public override IntPtr CurrentBuffer => _currentBufferPointer;
        public override uint BuffersQueued => (uint)_bufferQueue.Count;
        public override uint TotalBuffersReferenced => (uint)(_bufferQueue.Count + _processedBufferPointers.Count);

        public override double PositionInCurrentBuffer
        {
            get
            {
                long positionInSamples = _sourceVoice.State.SamplesPlayed - _lastNbSamplesPlayed;
                return (double)positionInSamples / _device.AudioParameters.SampleRate;
            }
        }

        public override float Volume
        {
            get
            {
                _sourceVoice.GetVolume(out float vol);
                return vol;
            }
            set => _sourceVoice.SetVolume(value);
        }

        private void OnBufferStart(IntPtr pointer)
        {
            _currentBufferPointer = pointer;
            _lastNbSamplesPlayed = _sourceVoice.State.SamplesPlayed;
        }

        private void OnBufferEnd(IntPtr pointer)
        {
            _currentBufferPointer = IntPtr.Zero;
            _processedBufferPointers.Enqueue(pointer);
            bool dequeued = _bufferQueue.TryDequeue(out IntPtr dequeuedPointer);
            Debug.Assert(dequeued);
            Debug.Assert(dequeuedPointer == pointer);
        }

        public override void Play()
        {
            _sourceVoice.Start();
        }

        public override bool TrySubmitBuffer(IntPtr data, uint size)
        {
            if (_sourceVoice.State.BuffersQueued == MaxQueuedBuffers)
            {
                return false;
            }

            _xaudioBuffer.Context = data;
            _xaudioBuffer.AudioDataPointer = data;
            _xaudioBuffer.AudioBytes = (int)size;
            _xaudioBuffer.Flags = BufferFlags.None;
            _sourceVoice.SubmitSourceBuffer(_xaudioBuffer, null);
            _bufferQueue.Enqueue(data);

            Interlocked.CompareExchange(ref _currentBufferPointer, data, IntPtr.Zero);
            return true;
        }

        public override void FlushBuffers()
        {
            _sourceVoice.FlushSourceBuffers();
            _currentBufferPointer = IntPtr.Zero;
        }

        public override void Stop()
        {
            _sourceVoice.Stop();
        }

        public override void Dispose()
        {
            Stop();
            FlushBuffers();
            _sourceVoice.DestroyVoice();
            _sourceVoice.Dispose();
        }
    }
}
