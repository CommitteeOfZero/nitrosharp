using System;
using System.Collections.Generic;
using System.Text;

namespace MoeGame.Framework.Audio
{
    public abstract class ResourceFactory
    {
        public abstract AudioSource CreateAudioSource();
    }
}
