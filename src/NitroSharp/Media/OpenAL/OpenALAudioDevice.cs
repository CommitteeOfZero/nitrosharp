using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenAL;

namespace NitroSharp.Media.OpenAL
{
    internal sealed class OpenALAudioDevice : AudioDevice
    {
        private readonly List<OpenALAudioSource> _audioSources = new();

        public OpenALAudioDevice(in AudioParameters audioParameters)
            : base(audioParameters)
        {
            Device = ALC10.alcOpenDevice("");
            if (Device == IntPtr.Zero) {
                return; // TODO
            }
            int[] attrList = {};
            Context = ALC10.alcCreateContext(Device, attrList);
            if (!ALC10.alcMakeContextCurrent(Context)) { // I don't know how this would work with multiple devices
                return; // TODO
            }
        }

        private IntPtr Context;
        public IntPtr Device { get; }

        public override OpenALAudioSource CreateAudioSource(int bufferSize = 16384, int bufferCount = 16)
        {
            var source = new OpenALAudioSource(this, bufferSize, bufferCount);
            _audioSources.Add(source);
            return source;
        }

        public override async ValueTask DisposeAsync()
        {
            await Task.WhenAll(_audioSources.Select(x => x.DisposeAsync().AsTask()));
            _audioSources.Clear();
            ALC10.alcMakeContextCurrent(IntPtr.Zero);
            ALC10.alcDestroyContext(Context);
            ALC10.alcCloseDevice(Device);
        }
    }
}
