using System;
using System.Collections.Generic;
using OpenAL;

namespace NitroSharp.Media.OpenAL
{
    public sealed class OpenALAudioSource : AudioSource
    {
        private readonly OpenALAudioDevice _device;
        private readonly AudioParameters _parameters;

        private uint _handle;

        private readonly Queue<IntPtr> _bufferQueue = new Queue<IntPtr>((int)MaxQueuedBuffers);
        private readonly Queue<uint> _freeAlBuffers = new Queue<uint>((int)MaxQueuedBuffers);
        private readonly uint[] _processedAlBuffers = new uint[MaxQueuedBuffers];

        public OpenALAudioSource(OpenALAudioDevice device)
        {
            _device = device;
            _parameters = device.AudioParameters;
            AL10.alGenSources(1, out _handle);
            _device.CheckLastError();
        }

        public override uint BuffersQueued => (uint)_bufferQueue.Count;
        public override uint TotalBuffersReferenced => (uint)(_bufferQueue.Count + _processedBufferPointers.Count);

        public override IntPtr CurrentBuffer
        {
            get
            {
                if (_bufferQueue.Count == 0)
                {
                    return IntPtr.Zero;
                }

                DequeueProcessedBuffers();
                return _bufferQueue.Count > 0 ? _bufferQueue.Peek() : IntPtr.Zero;
            }
        }

        public override double PositionInCurrentBuffer
        {
            get
            {
                AL10.alGetSourcei(_handle, AL11.AL_SAMPLE_OFFSET, out int sampleOffset);
                _device.Debug_CheckLastError();

                return (double)sampleOffset / _parameters.SampleRate;
            }
        }

        public override float Volume
        {
            get
            {
                AL10.alGetSourcef(_handle, AL10.AL_GAIN, out float volume);
                _device.Debug_CheckLastError();
                return volume;
            }
            set
            {
                AL10.alSourcef(_handle, AL10.AL_GAIN, value);
                _device.Debug_CheckLastError();
            }
        }

        public override void Play()
        {
        }

        public override bool TrySubmitBuffer(IntPtr data, uint size)
        {
            DequeueProcessedBuffers();

            AL10.alGetSourcei(_handle, AL10.AL_BUFFERS_QUEUED, out int nbQueued);
            _device.Debug_CheckLastError();
            if (nbQueued >= MaxQueuedBuffers)
            {
                return false;
            }

            uint currentBuf = GetFreeAlBuffer();
            AL10.alBufferData(currentBuf, _device.SampleFormat, data, (int)size, (int)_parameters.SampleRate);
            _device.Debug_CheckLastError();
            AL10.alSourceQueueBuffers(_handle, 1, ref currentBuf);
            _device.Debug_CheckLastError();

            _bufferQueue.Enqueue(data);

            int oldNbQueued = nbQueued;
            if (oldNbQueued == 0)
            {
                AL10.alSourcePlay(_handle);
                _device.CheckLastError();
            }

            return true;
        }

        public override bool TryDequeueProcessedBuffer(out IntPtr pointer)
        {
            DequeueProcessedBuffers();
            return base.TryDequeueProcessedBuffer(out pointer);
        }

        private void DequeueProcessedBuffers()
        {
            AL10.alGetSourcei(_handle, AL10.AL_BUFFERS_PROCESSED, out int nbProcessed);
            _device.Debug_CheckLastError();

            AL10.alSourceUnqueueBuffers(_handle, nbProcessed, _processedAlBuffers);
            _device.Debug_CheckLastError();

            for (int i = 0; i < nbProcessed; i++)
            {
                _processedBufferPointers.Enqueue(_bufferQueue.Dequeue());
                _freeAlBuffers.Enqueue(_processedAlBuffers[i]);
            }
        }

        private uint GetFreeAlBuffer()
        {
            uint buffer;
            if (_freeAlBuffers.Count > 0)
            {
                buffer = _freeAlBuffers.Dequeue();
            }
            else
            {
                AL10.alGenBuffers(1, out buffer);
                _device.Debug_CheckLastError();
            }

            return buffer;
        }

        public override void Stop()
        {
            AL10.alSourceStop(_handle);
        }

        public override void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        private void Free()
        {
            Stop();
            while (_freeAlBuffers.Count > 0)
            {
                uint handle = _freeAlBuffers.Dequeue();
                AL10.alDeleteBuffers(1, ref handle);
            }

            AL10.alDeleteSources(1, ref _handle);
        }

        public override void FlushBuffers()
        {
            DequeueProcessedBuffers();
            _bufferQueue.Clear();
        }

        ~OpenALAudioSource()
        {
            Free();
        }
    }
}
