using SharpDX.XAudio2;

namespace SciAdvNet.MediaLayer.Audio.XAudio
{
    public class XAudio2AudioEngine : AudioEngine
    {
        internal XAudio2 Device { get; }
        private MasteringVoice _masteringVoice;

        public XAudio2AudioEngine()
        {
            var flags = XAudio2Flags.None;
            Device = new XAudio2(flags, ProcessorSpecifier.DefaultProcessor);
            _masteringVoice = new MasteringVoice(Device);

            ResourceFactory = new XAudio2ResourceFactory(this);
        }

        public override void Dispose()
        {
            _masteringVoice.Dispose();
            Device.Dispose();
        }
    }
}
