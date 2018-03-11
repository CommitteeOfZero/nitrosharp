using System;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal struct ResourceLayoutSetPair : IEquatable<ResourceLayoutSetPair>
    {
        public ResourceLayout ResourceLayout;
        public ResourceSetDescription ResourceSetDescription;

        public bool Equals(ResourceLayoutSetPair other)
        {
            return ResourceLayout.Equals(other.ResourceLayout)
                && ResourceSetDescription.Equals(other.ResourceSetDescription);
        }

        public override int GetHashCode()
        {
            return HashHelper.Combine(ResourceLayout.GetHashCode(), ResourceSetDescription.GetHashCode());
        }
    }
}
