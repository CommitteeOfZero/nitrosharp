using System;
using System.Collections.Generic;
using System.Text;

namespace HoppyFramework.Audio
{
    public abstract class ResourceFactory
    {
        public abstract AudioSource CreateAudioSource();
    }
}
