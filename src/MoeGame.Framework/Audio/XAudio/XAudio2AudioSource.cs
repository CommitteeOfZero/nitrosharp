using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;

namespace MoeGame.Framework.Audio.XAudio
{
    public sealed class XAudio2AudioSource : AudioSource
    {
        private readonly XAudio2AudioEngine _engine;
        private SourceVoice _sourceVoice;
        private float _volume;

        internal XAudio2AudioSource(XAudio2AudioEngine engine, uint bufferSize)
            : base(engine, bufferSize)
        {
            _engine = engine;
            var waveFormat = new WaveFormat(engine.SampleRate, engine.BitDepth, engine.ChannelCount);
            _sourceVoice = new SourceVoice(engine.Device, waveFormat);
            _sourceVoice.BufferEnd += RaiseBufferEnd;
        }

        public override event EventHandler<AudioBuffer> BufferEnd;

        private void RaiseBufferEnd(IntPtr context)
        {
            int id = (int)context;
            BufferEnd?.Invoke(this, _buffers[id]);
        }

        public override float Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    _sourceVoice.SetVolume(value);
                }
            }
        }

        internal override void StartAcceptingBuffers()
        {
            _sourceVoice.Start();
        }

        internal override void StopAcceptingBuffers()
        {
            _sourceVoice.Stop();
        }

        internal override void FlushBuffers()
        {
            _sourceVoice.FlushSourceBuffers();
        }

        internal override void AcceptBuffer(AudioBuffer buffer)
        {
            base.AcceptBuffer(buffer);

            var dataPointer = new DataPointer(buffer.StartPointer, buffer.Position);
            var xaudio2Buffer = new SharpDX.XAudio2.AudioBuffer(dataPointer);
            xaudio2Buffer.Context = (IntPtr)buffer.Id;

            _sourceVoice.SubmitSourceBuffer(xaudio2Buffer, null);
        }

        public override void Dispose()
        {
            Stop();
            _sourceVoice.Dispose();
        }
    }
}
