using System.Threading.Tasks;

namespace NitroSharp.Media.NullAudio
{
    internal sealed class NullAudioDevice : AudioDevice
    {
        public NullAudioDevice(in AudioParameters audioParameters)
            : base(in audioParameters)
        {
        }

        public override AudioSource CreateAudioSource(int bufferSize = 16384, int bufferCount = 16)
        {
            return new NullAudioSource();
        }

        public override ValueTask DisposeAsync() => default;
    }
}
