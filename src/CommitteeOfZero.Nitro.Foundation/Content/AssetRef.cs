using System;

namespace CommitteeOfZero.Nitro.Foundation.Content
{
    public sealed class AssetRef<T> : IDisposable
    {
        private readonly ContentManager _contentManager;
        private bool _alive;

        public AssetRef(AssetId id, ContentManager contentManager)
        {
            Id = id;
            _contentManager = contentManager;
            _alive = true;
        }

        public AssetId Id { get; }
        public T Asset
        {
            get
            {
                return _alive ? _contentManager.InternalGetCached<T>(Id)
                    : throw new InvalidOperationException($"Attempted to use an asset reference that has been released. AssetId: {Id}.");
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
