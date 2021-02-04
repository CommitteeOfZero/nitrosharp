using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpDX.XAudio2;

namespace NitroSharp.Media.XAudio2
{
    internal sealed class XAudio2AudioDevice : AudioDevice
    {
        private static class CoreRTWorkaround
        {
            public static void InitializeCOM()
            {
                CoInitializeEx(IntPtr.Zero, COINIT.COINIT_MULTITHREADED);
            }

            [DllImport("ole32.dll", SetLastError = true)]
            private static extern int CoInitializeEx(
                [In, Optional] IntPtr pvReserved,
                [In] COINIT dwCoInit
            );

            private enum COINIT : uint
            {
                COINIT_MULTITHREADED = 0x0,
                COINIT_APARTMENTTHREADED = 0x2,
                COINIT_DISABLE_OLE1DDE = 0x4,
                COINIT_SPEED_OVER_MEMORY = 0x8,
            }
        }

        private readonly MasteringVoice _masteringVoice;
        private readonly List<XAudio2AudioSource> _audioSources = new();

        public XAudio2AudioDevice(in AudioParameters audioParameters)
            : base(audioParameters)
        {
            CoreRTWorkaround.InitializeCOM();

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
