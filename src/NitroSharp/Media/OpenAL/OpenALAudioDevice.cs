using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenAL;

namespace NitroSharp.Media.OpenAL
{
    public sealed class OpenALAudioDevice : AudioDevice
    {
        private IntPtr _device;
        private IntPtr _context;

        public OpenALAudioDevice(in AudioParameters audioParameters) : base(audioParameters)
        {
            SampleFormat = GetSampleFormat(audioParameters.ChannelLayout);
            _device = ALC10.alcOpenDevice(null);
            CheckLastError();
            _context = ALC10.alcCreateContext(_device, null);
            CheckLastError();
            ALC10.alcMakeContextCurrent(_context);
            CheckLastError();
        }

        public int SampleFormat { get; }

        public override AudioSource CreateAudioSource()
        {
            return new OpenALAudioSource(this);
        }

        private static int GetSampleFormat(ChannelLayout channelLayout)
        {
            return channelLayout == ChannelLayout.Mono
                ? AL10.AL_FORMAT_MONO16
                : AL10.AL_FORMAT_STEREO16;
        }

        public override void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        private void Free()
        {
            ALC10.alcMakeContextCurrent(IntPtr.Zero);
            ALC10.alcDestroyContext(_context);
            ALC10.alcCloseDevice(_device);
            _context = _device = IntPtr.Zero;
        }

        [Conditional("DEBUG")]
        [DebuggerNonUserCode]
        public void Debug_CheckLastError()
        {
            CheckLastErrorCore();   
        }

        public void CheckLastError()
        {
            CheckLastErrorCore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckLastErrorCore()
        {
            int error = ALC10.alcGetError(_device);
            if (error != ALC10.ALC_NO_ERROR)
            {
                throw new Exception();
            }
        }

        ~OpenALAudioDevice()
        {
            Free();
        }
    }
}
