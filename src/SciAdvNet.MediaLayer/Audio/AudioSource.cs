using System;
using System.Collections.Generic;
using System.Text;

namespace SciAdvNet.MediaLayer.Audio
{
    public abstract class AudioSource
    {
        public abstract float Volume { get; set; }
        public abstract void Play(AudioFile file);
    }
}
