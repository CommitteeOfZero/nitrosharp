using System;
using System.Runtime.InteropServices;
using SharpDX.XAudio2;

namespace NitroSharp.Media.XAudio2
{
    public sealed class XAudio2AudioDevice : AudioDevice
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

        public XAudio2AudioDevice(in AudioParameters audioParameters) : base(audioParameters)
        {
            CoreRTWorkaround.InitializeCOM();

            Device = new SharpDX.XAudio2.XAudio2(XAudio2Flags.None, ProcessorSpecifier.DefaultProcessor);
            _masteringVoice = new MasteringVoice(Device, audioParameters.ChannelCount, (int)audioParameters.SampleRate);
        }

        public SharpDX.XAudio2.XAudio2 Device { get; }

        public override AudioSource CreateAudioSource()
        {
            return new XAudio2AudioSource(this);
        }

        public override void Dispose()
        {
            _masteringVoice.DestroyVoice();
            _masteringVoice.Dispose();
            Device.StopEngine();
            Device.Dispose();
        }
    }
}
