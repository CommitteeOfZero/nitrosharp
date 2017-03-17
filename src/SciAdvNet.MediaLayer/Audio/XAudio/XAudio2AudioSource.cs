using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;

namespace SciAdvNet.MediaLayer.Audio.XAudio
{
    public class XAudio2AudioSource : AudioSource
    {
        private readonly XAudio2AudioEngine _engine;
        private SourceVoice _sourceVoice;

        internal XAudio2AudioSource(XAudio2AudioEngine engine) : base(engine)
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
            get => _sourceVoice.Volume;
            set => _sourceVoice.SetVolume(value);
        }

        internal override void StartAcceptingBuffers()
        {
            _sourceVoice.Start();
        }

        internal override void AcceptBuffer(AudioBuffer buffer)
        {
            base.AcceptBuffer(buffer);

            var dataPointer = new DataPointer(buffer.StartPointer, buffer.Position);
            var xaudio2Buffer = new SharpDX.XAudio2.AudioBuffer(dataPointer);
            xaudio2Buffer.Context = (IntPtr)buffer.Id;

            _sourceVoice.SubmitSourceBuffer(xaudio2Buffer, null);
        }
    }
}
