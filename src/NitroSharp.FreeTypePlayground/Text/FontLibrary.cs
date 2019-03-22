using System;
using System.Collections.Generic;
using FreeTypeBindings;

namespace NitroSharp.Text
{
    internal sealed class FontLibrary : IDisposable
    {
        private sealed class FreeTypeInstance : IDisposable
        {
            private IntPtr _handle;

            public FreeTypeInstance()
            {
                FT.FT_Init_FreeType(out _handle);
            }

            public IntPtr Handle => _handle;

            public void Dispose()
            {
                Destroy();
                GC.SuppressFinalize(this);
            }

            private void Destroy()
            {
                FT.FT_Done_FreeType(_handle);
                _handle = IntPtr.Zero;
            }

            ~FreeTypeInstance()
            {
                Destroy();
            }
        }

        private readonly FreeTypeInstance _freetype;
        private readonly Dictionary<string, List<FontFace>> _instances;
        private readonly Dictionary<string, FontFamily> _families;

        public FontLibrary()
        {
            _freetype = new FreeTypeInstance();
            _instances = new Dictionary<string, List<FontFace>>(StringComparer.OrdinalIgnoreCase);
            _families = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);
        }

        public FontFamily RegisterFont(string filePath)
        {
            FontFace font = Load(filePath);
            string familyName = font.FontFamily;
            if (!_instances.ContainsKey(familyName))
            {
                var list = new List<FontFace>(2);
                list.Add(font);
                _instances[familyName] = list;
            }

            if (!_families.TryGetValue(familyName, out FontFamily family))
            {
                family = new FontFamily(familyName, this);
                _families[familyName] = family;
            }

            return family;
        }

        public void RegisterFonts(ReadOnlySpan<string> filePaths)
        {
            foreach (string path in filePaths)
            {
                RegisterFont(path);
            }
        }

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

        private unsafe FontFace Load(string path)
        {
            FT.CheckResult(FT.FT_New_Face(_freetype.Handle, path, 0, out Face* face));
            return new FontFace(_freetype.Handle, face);
        }

        public void Dispose()
        {
            foreach (List<FontFace> list in _instances.Values)
            {
                foreach (FontFace face in list)
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
