using System;

namespace NitroSharp.Content
{
    internal sealed class AssetRef<T> : IDisposable
    {
        private readonly ContentManager _contentManager;
        private bool _alive;
        private T _asset;

        public AssetRef(AssetId id, ContentManager contentManager)
        {
            Id = id;
            _contentManager = contentManager;
            _alive = true;
            _asset = default(T);
        }

        public AssetId Id { get; }
        public T Asset
        {
            get
            {
                //if (!_alive)
                //{
                //    throw new InvalidOperationException($"Attempted to use an asset reference that has been released. AssetId: {Id}.");
                //}

                if (_asset == null)
                {
                    _asset = _contentManager.InternalGetCached<T>(Id);
                }

                return _asset;
            }
        }

        public override string ToString() => Id;

        public void Dispose()
        {
            _alive = false;
            _contentManager.ReleaseReference(Id);
        }
    }
}
