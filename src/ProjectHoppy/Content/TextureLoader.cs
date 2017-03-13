using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectHoppy.Content
{
    public class TextureLoader : ContentLoader
    {
        private const string JpgSignature = "ÿØÿà";
        private const string PngSignature = ".PNG";

        private readonly string[] _extensions;
        private readonly string[] _signatures;
        private readonly SciAdvNet.MediaLayer.Graphics.ResourceFactory _resourceFactory;

        public TextureLoader(SciAdvNet.MediaLayer.Graphics.ResourceFactory resourceFactory)
        {
            _resourceFactory = resourceFactory;
            _signatures = new[] { JpgSignature, PngSignature };
            _extensions = new[] { ".jpg" };
        }

        public override IEnumerable<string> FileSignatures => _signatures;
        public override IEnumerable<string> FileExtensions => _extensions;

        public override object Load(Stream stream)
        {
            return _resourceFactory.CreateTexture(stream);
        }
    }
}
