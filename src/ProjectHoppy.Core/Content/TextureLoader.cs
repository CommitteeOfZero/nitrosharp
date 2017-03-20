using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectHoppy.Core.Content
{
    public class TextureLoader : ContentLoader
    {
        private const string JpgSignature = "ÿØÿà";
        private const string PngSignature = ".PNG";

        private readonly string[] _signatures;
        private readonly SciAdvNet.MediaLayer.Graphics.ResourceFactory _resourceFactory;

        public TextureLoader(SciAdvNet.MediaLayer.Graphics.ResourceFactory resourceFactory)
        {
            _resourceFactory = resourceFactory;
            _signatures = new[] { JpgSignature, PngSignature };
        }

        //public override IEnumerable<string> FileSignatures => _signatures;

        public override object Load(Stream stream)
        {
            return _resourceFactory.CreateTexture(stream);
        }
    }
}
