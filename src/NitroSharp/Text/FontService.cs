using System;
using System.Collections.Generic;
using FreeTypeBindings;

namespace NitroSharp.Text
{
    internal sealed class FontService : IDisposable
    {
        private readonly FreeTypeInstance _freetype;
        private readonly Dictionary<string, List<FontFace>> _instances;
        private readonly Dictionary<string, FontFamily> _families;

        public FontService()
        {
            _freetype = new FreeTypeInstance();
            _instances = new Dictionary<string, List<FontFace>>(StringComparer.OrdinalIgnoreCase);
            _families = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);
        }

        public FontFamily RegisterFont(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var font = Load(filePath);
            string familyName = font.FontFamily;
            if (!_instances.ContainsKey(familyName))
            {
                var list = new List<FontFace>(2);
                list.Add(font);
                _instances[familyName] = list;
            }

            if (!_families.TryGetValue(familyName, out var family))
            {
                family = new FontFamily(familyName, this);
                _families[familyName] = family;
            }

            return family;
        }

        public void RegisterFonts(IEnumerable<string> filePaths)
        {
            foreach (string path in filePaths)
            {
                RegisterFont(path);
            }
        }

        public FontFamily GetFontFamily(string familyName) => _families[familyName];

        public FontFace Find(string fontFamily, FontStyle fontStyle)
        {
            List<FontFace> collection = _instances[fontFamily];
            foreach (FontFace font in collection)
            {
                if (font.Style == fontStyle)
                {
                    return font;
                }
            }

            return ThrowNotFound(fontFamily, fontStyle);
        }

        private FontFace Load(string path)
        {
            unsafe
            {
                FT.CheckResult(FT.FT_New_Face(_freetype.Handle, path, 0, out var face));
                return new FontFace(face);
            }
        }

        public void Dispose()
        {
            foreach (var list in _instances.Values)
            {
                foreach (var face in list)
                {
                    face.Dispose();
                }
            }

            _instances.Clear();
            _families.Clear();
            _freetype.Dispose();
        }

        private FontFace ThrowNotFound(string fontFamily, FontStyle fontStyle)
            => throw new ArgumentException($"Could not find font {{FontFamily = {fontFamily}, FontStyle = {fontStyle}}}.");
    }
}
