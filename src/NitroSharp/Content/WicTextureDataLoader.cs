using System.IO;
using SharpDX.WIC;

namespace NitroSharp.Content
{
    internal sealed class WicTextureDataLoader : ContentLoader
    {
        private readonly ImagingFactory _wicFactory;

        public WicTextureDataLoader(ContentManager content, ImagingFactory wicFactory) : base(content)
        {
            _wicFactory = wicFactory;
        }

        public override object Load(Stream stream)
        {
            //using (stream)
            {
                var decoder = new BitmapDecoder(_wicFactory, stream, DecodeOptions.CacheOnDemand);
                var pixelFormatConverter = new FormatConverter(_wicFactory);
                var frame = decoder.GetFrame(0);
                pixelFormatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppRGBA);
                return new WicTextureData(decoder, frame, pixelFormatConverter);
            }
        }
    }
}
