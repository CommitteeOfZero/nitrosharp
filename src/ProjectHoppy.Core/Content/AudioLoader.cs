using SciAdvNet.MediaLayer.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProjectHoppy.Core.Content
{
    public class AudioLoader : ContentLoader
    {
        private readonly SciAdvNet.MediaLayer.Audio.ResourceFactory _resourceFactory;

        public AudioLoader(SciAdvNet.MediaLayer.Audio.ResourceFactory resourceFactory)
        {
            _resourceFactory = resourceFactory;
        }

        //public override IEnumerable<string> FileSignatures => throw new NotImplementedException();

        public override object Load(Stream stream)
        {
            return new FFmpegAudioStream(stream);
        }
    }
}
