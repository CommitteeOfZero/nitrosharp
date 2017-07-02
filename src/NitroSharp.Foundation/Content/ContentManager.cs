using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NitroSharp.Foundation.Content
{
    public class ContentManager : IDisposable
    {
        private readonly Dictionary<Type, ContentLoader> _contentLoaders;
        private readonly Dictionary<AssetId, (object asset, int refCount)> _loadedAssets;

        public ContentManager(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            _contentLoaders = new Dictionary<Type, ContentLoader>();
            _loadedAssets = new Dictionary<AssetId, (object asset, int refCount)>();
        }

        public ContentManager() : this(string.Empty)
        {
        }

        public string RootDirectory { get; }

        public bool IsLoaded(AssetId assetId) => _loadedAssets.ContainsKey(assetId);
        public bool Exists(AssetId assetId)
        {
            Stream stream = null;
            try
            {
                stream = OpenStream(assetId);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public AssetRef<T> Get<T>(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cachedItem))
            {
                IncrementRefCount(assetId, cachedItem);
            }
            else
            {
                Load<T>(assetId);
            }

            return new AssetRef<T>(assetId, this);
        }

        internal T InternalGetCached<T>(AssetId assetId) => (T)_loadedAssets[assetId].asset;

        public void RegisterContentLoader(Type t, ContentLoader loader)
        {
            _contentLoaders[t] = loader;
        }

        private T Load<T>(AssetId assetId) => (T)Load(assetId, typeof(T));
        private object Load(AssetId assetId) => Load(assetId, contentType: null);

        private object Load(AssetId assetId, Type contentType)
        {
            var stream = OpenStream(assetId);
            {
                return Load(stream, assetId, contentType);
            }
        }

        private object Load(Stream stream, AssetId assetId, Type contentType)
        {
            if (stream == null)
            {
                throw new ContentLoadException($"Failed to load asset '{assetId}': file not found.");
            }

            //if (contentType == null)
            //{
            //    using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            //    {
            //        contentType = IdentifyContentType(reader);
            //    }
            //}

            if (contentType == null || !_contentLoaders.TryGetValue(contentType, out var loader))
            {
                throw UnsupportedFormat(assetId);
            }

            object asset = loader.Load(stream);
            _loadedAssets[assetId] = (asset, 1);
            return asset;
        }

        //private Type IdentifyContentType(BinaryReader reader)
        //{
        //    Type contentType = null;
        //    foreach (var pair in _contentLoaders)
        //    {
        //        var loader = pair.Value;
        //        bool match = loader.IsSupportedContentType(reader);
        //        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        //        if (match)
        //        {
        //            contentType = pair.Key;
        //            break;
        //        }
        //    }

        //    return contentType;
        //}

        private int IncrementRefCount(AssetId assetId, (object asset, int refCount) cachedItem)
        {
            _loadedAssets[assetId] = (cachedItem.asset, cachedItem.refCount + 1);
            return cachedItem.refCount + 1;
        }

        private int DecrementRefCount(AssetId assetId, (object asset, int refCount) cachedItem)
        {
            _loadedAssets[assetId] = (cachedItem.asset, cachedItem.refCount - 1);
            return cachedItem.refCount - 1;
        }

        internal void ReleaseReference(AssetId assetId)
        {
            if (_loadedAssets.TryGetValue(assetId, out var cachedItem))
            {
                int newRefCount = DecrementRefCount(assetId, cachedItem);
                if (newRefCount == 0)
                {
                    (cachedItem.asset as IDisposable)?.Dispose();
                    _loadedAssets.Remove(assetId);

                    Debug.WriteLine(assetId + " disposed");
                }
            }
        }

        protected virtual Stream OpenStream(string path)
        {
            string fullPath = Path.Combine(RootDirectory, path);
            return File.OpenRead(fullPath);
        }

        private Exception UnsupportedFormat(string path)
        {
            return new ContentLoadException($"Failed to load asset '{path}': unsupported format.");
        }

        public void Dispose()
        {
            foreach (var cachedItem in _loadedAssets.Values)
            {
                (cachedItem.asset as IDisposable)?.Dispose();
            }

            _loadedAssets.Clear();
        }
    }
}
