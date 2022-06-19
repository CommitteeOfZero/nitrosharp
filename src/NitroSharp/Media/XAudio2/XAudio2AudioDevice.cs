using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.XAudio2;

namespace NitroSharp.Media.XAudio2
{
    internal sealed class XAudio2AudioDevice : AudioDevice
    {
        private readonly MasteringVoice _masteringVoice;
        private readonly List<XAudio2AudioSource> _audioSources = new();

        public XAudio2AudioDevice(in AudioParameters audioParameters)
            : base(audioParameters)
        {
            Device = new SharpDX.XAudio2.XAudio2(
                XAudio2Flags.None,
                ProcessorSpecifier.DefaultProcessor
            );
            _masteringVoice = new MasteringVoice(
                Device,
                audioParameters.ChannelCount,
                (int)audioParameters.SampleRate
            );
        }

        public SharpDX.XAudio2.XAudio2 Device { get; }

        public override XAudio2AudioSource CreateAudioSource(int bufferSize = 16384, int bufferCount = 16)
        {
            var source = new XAudio2AudioSource(this, bufferSize, bufferCount);
            _audioSources.Add(source);
            return source;
        }

        public override async ValueTask DisposeAsync()
        {
            await Task.WhenAll(_audioSources.Select(x => x.DisposeAsync().AsTask()));
            _audioSources.Clear();
            _masteringVoice.DestroyVoice();
            _masteringVoice.Dispose();
            Device.StopEngine();
            Device.Dispose();
        }
    }
}
