using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NitroSharp.Common;
using SharpGen.Runtime;
using Vortice.XAudio2;

namespace NitroSharp.Media.XAudio2;

internal sealed class XAudio2AudioDevice : AudioDevice
{
    private readonly IXAudio2MasteringVoice _masteringVoice;
    private readonly List<XAudio2AudioSource> _audioSources = [];

    public XAudio2AudioDevice(in AudioParameters audioParameters)
        : base(audioParameters)
    {
        Result result = Vortice.XAudio2.XAudio2.XAudio2Create(out IXAudio2? device);
        result.CheckError();
        Device = device.NotNull();

        _masteringVoice = Device.CreateMasteringVoice(
            audioParameters.ChannelCount,
            (int)audioParameters.SampleRate
        );
    }

    public IXAudio2 Device { get; }
    public override AudioBackend Backend => AudioBackend.XAudio2;

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
