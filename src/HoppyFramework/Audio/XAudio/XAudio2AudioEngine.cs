using SharpDX.XAudio2;

namespace HoppyFramework.Audio.XAudio
{
    public class XAudio2AudioEngine : AudioEngine
    {
        private readonly MasteringVoice _masteringVoice;

        public XAudio2AudioEngine(int bitDepth, int sampleRate, int channelCount)
            : base(bitDepth, sampleRate, channelCount)
        {
            Device = new XAudio2(XAudio2Flags.None, ProcessorSpecifier.DefaultProcessor);
            _masteringVoice = new MasteringVoice(Device);
            ResourceFactory = new XAudio2ResourceFactory(this);
        }

        internal XAudio2 Device { get; }

        public override void Dispose()
        {
            _masteringVoice.Dispose();
            Device.Dispose();
        }
    }
}
