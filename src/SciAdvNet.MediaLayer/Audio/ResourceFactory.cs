using System;
using System.Collections.Generic;
using System.Text;

namespace SciAdvNet.MediaLayer.Audio
{
    public abstract class ResourceFactory
    {
        public abstract AudioSource CreateAudioSource();
    }
}
