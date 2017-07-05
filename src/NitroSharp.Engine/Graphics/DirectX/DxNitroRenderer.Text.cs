using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.IO;

namespace NitroSharp.Graphics
{
    public sealed partial class DxNitroRenderer
    {
        private TextLayout _textLayout;
        private TextRenderer _customTextRenderer;
        private TextFormat _textFormat;

        private (TextRange range, TextDrawingContext context) _animatedRegion;
        private (TextRange range, TextDrawingContext context) _visibleRegion;

        private UserFontLoader _userFontLoader;
        private FontCollection _userFontCollection;

        private void CreateTextResources(IEnumerable<string> userFontLocations)
        {
            _customTextRenderer = new CustomTextRenderer(_rc.DeviceContext, _rc.ColorBrush);

            _userFontLoader = new UserFontLoader(_rc.DWriteFactory, userFontLocations);
            _userFontCollection = new FontCollection(_rc.DWriteFactory, _userFontLoader, _userFontLoader.Key);

            _textFormat = new TextFormat(_rc.DWriteFactory, "Noto Sans CJK JP",
                _userFontCollection, FontWeight.Regular, FontStyle.Normal, FontStretch.Normal, 26);

            _visibleRegion.context = new TextDrawingContext { OpacityOverride = 1.0f };
            _animatedRegion.context = new TextDrawingContext();
        }

        public void DrawText(TextVisual text)
        {
            if (_textLayout == null || _textLayout.IsDisposed)
            {
                _textLayout = new TextLayout(_rc.DWriteFactory, text.Text, _textFormat, text.Measure().Width, text.Measure().Height);
            }

            _rc.ColorBrush.Color = text.Color;
            _rc.ColorBrush.Opacity = 0;

            if (_animatedRegion.range != text.AnimatedRegion)
            {
                var range = _animatedRegion.range = text.AnimatedRegion;
                if (range.Length > 0)
                {
                    _textLayout.SetDrawingEffect(_animatedRegion.context, new SharpDX.DirectWrite.TextRange(range.RangeStart, range.Length));
                }
            }

            if (_visibleRegion.range != text.VisibleRegion)
            {
                var range = _visibleRegion.range = text.VisibleRegion;
                if (range.Length > 0)
                {
                    _textLayout.SetDrawingEffect(_visibleRegion.context, new SharpDX.DirectWrite.TextRange(range.RangeStart, range.Length));
                }
            }

            _animatedRegion.context.OpacityOverride = text.AnimatedOpacity;
            _textLayout.Draw(_customTextRenderer, 0, 0);
        }

        public void Free(TextVisual textVisual)
        {
            _textLayout.Dispose();
        }

        private sealed class UserFontLoader : CallbackBase, FontCollectionLoader, FontFileLoader
        {
            private readonly List<UserFontFileStream> _fontStreams = new List<UserFontFileStream>();
            private readonly List<UserFontFileEnumerator> _enumerators = new List<UserFontFileEnumerator>();
            private readonly Factory _factory;

            public UserFontLoader(Factory factory, IEnumerable<string> fontLocations)
            {
                _factory = factory;

                foreach (string location in fontLocations)
                {
                    foreach (var fontFileName in Directory.EnumerateFiles(location))
                    {
                        var bytes = File.ReadAllBytes(fontFileName);
                        var stream = new DataStream(bytes.Length, true, true);
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Seek(0, SeekOrigin.Begin);
                        _fontStreams.Add(new UserFontFileStream(new DataStream(stream)));
                    }
                }

                // Build a Key storage that stores the index of the font
                Key = new DataStream(sizeof(int) * _fontStreams.Count, true, true);
                for (int i = 0; i < _fontStreams.Count; i++)
                {
                    Key.Write(i);
                }

                Key.Seek(0, SeekOrigin.Begin);

                _factory.RegisterFontFileLoader(this);
                _factory.RegisterFontCollectionLoader(this);
            }


            /// <summary>
            /// Gets the key used to identify the FontCollection as well as storing index for fonts.
            /// </summary>
            /// <value>The key.</value>
            public DataStream Key { get; }

            /// <summary>
            /// Creates a font file enumerator object that encapsulates a collection of font files. The font system calls back to this interface to create a font collection.
            /// </summary>
            /// <param name="factory">Pointer to the <see cref="SharpDX.DirectWrite.Factory"/> object that was used to create the current font collection.</param>
            /// <param name="collectionKey">A font collection key that uniquely identifies the collection of font files within the scope of the font collection loader being used. The buffer allocated for this key must be at least  the size, in bytes, specified by collectionKeySize.</param>
            /// <returns>
            /// a reference to the newly created font file enumerator.
            /// </returns>
            public FontFileEnumerator CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey)
            {
                var enumerator = new UserFontFileEnumerator(factory, this, collectionKey);
                _enumerators.Add(enumerator);

                return enumerator;
            }

            /// <summary>
            /// Creates a font file stream object that encapsulates an open file resource.
            /// </summary>
            /// <param name="fontFileReferenceKey">A reference to a font file reference key that uniquely identifies the font file resource within the scope of the font loader being used. The buffer allocated for this key must at least be the size, in bytes, specified by  fontFileReferenceKeySize.</param>
            /// <returns>
            /// a reference to the newly created <see cref="SharpDX.DirectWrite.FontFileStream"/> object.
            /// </returns>
            /// <remarks>
            /// The resource is closed when the last reference to fontFileStream is released.
            /// </remarks>
            /// <unmanaged>HRESULT IDWriteFontFileLoader::CreateStreamFromKey([In, Buffer] const void* fontFileReferenceKey,[None] int fontFileReferenceKeySize,[Out] IDWriteFontFileStream** fontFileStream)</unmanaged>
            public FontFileStream CreateStreamFromKey(DataPointer fontFileReferenceKey)
            {
                var index = Utilities.Read<int>(fontFileReferenceKey.Pointer);
                return _fontStreams[index];
            }
        }

        private sealed class UserFontFileEnumerator : CallbackBase, FontFileEnumerator
        {
            private Factory _factory;
            private FontFileLoader _loader;
            private DataStream keyStream;
            private FontFile _currentFontFile;

            public UserFontFileEnumerator(Factory factory, FontFileLoader loader, DataPointer key)
            {
                _factory = factory;
                _loader = loader;
                keyStream = new DataStream(key.Pointer, key.Size, canRead: true, canWrite: false);
            }

            public FontFile CurrentFontFile
            {
                get
                {
                    ((IUnknown)_currentFontFile).AddReference();
                    return _currentFontFile;
                }
            }

            public bool MoveNext()
            {
                bool moveNext = keyStream.RemainingLength != 0;
                if (moveNext)
                {
                    _currentFontFile?.Dispose();
                    _currentFontFile = new FontFile(_factory, keyStream.PositionPointer, 4, _loader);
                    keyStream.Position += 4;
                }

                return moveNext;
            }
        }

        private sealed class UserFontFileStream : CallbackBase, FontFileStream
        {
            private readonly DataStream _stream;

            public UserFontFileStream(DataStream stream)
            {
                _stream = stream;
            }

            /// <summary>
            /// Reads a fragment from a font file.
            /// </summary>
            /// <param name="fragmentStart">When this method returns, contains an address of a reference to the start of the font file fragment. This parameter is passed uninitialized.</param>
            /// <param name="fileOffset">The offset of the fragment, in bytes, from the beginning of the font file.</param>
            /// <param name="fragmentSize">The size of the file fragment, in bytes.</param>
            /// <param name="fragmentContext">When this method returns, contains the address of</param>
            /// <remarks>
            /// Note that ReadFileFragment implementations must check whether the requested font file fragment is within the file bounds. Otherwise, an error should be returned from ReadFileFragment.
            /// DirectWrite may invoke <see cref="SharpDX.DirectWrite.FontFileStream"/> methods on the same object from multiple threads simultaneously.
            /// Therefore, ReadFileFragment implementations that rely on internal mutable state must serialize access to such state across multiple threads.
            /// For example, an implementation that uses separate Seek and Read operations to read a file fragment must place the code block containing Seek and Read calls under a lock or a critical section.
            /// </remarks>
            public void ReadFileFragment(out IntPtr fragmentStart, long fileOffset, long fragmentSize, out IntPtr fragmentContext)
            {
                lock (this)
                {
                    fragmentContext = IntPtr.Zero;
                    _stream.Position = fileOffset;
                    fragmentStart = _stream.PositionPointer;
                }
            }

            public void ReleaseFileFragment(IntPtr fragmentContext)
            {
                // Nothing to release. No context are used
            }

            public long GetFileSize() => _stream.Length;
            public long GetLastWriteTime() => 0;
        }
    }
}
