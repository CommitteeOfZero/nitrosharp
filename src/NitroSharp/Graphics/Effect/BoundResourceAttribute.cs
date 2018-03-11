using System;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class BoundResourceAttribute : Attribute
    {
        public BoundResourceAttribute(ResourceKind kind, ShaderStages shaderStages, uint resourceSet = 0)
            : this(null, kind, shaderStages, resourceSet)
        {
        }

        public BoundResourceAttribute(string name, ResourceKind kind, ShaderStages shaderStages, uint resourceSet = 0)
        {
            ResourceName = name;
            ResourceKind = kind;
            ShaderStages = shaderStages;
            ResourceSet = resourceSet;
        }

        public string ResourceName { get; }
        public ResourceKind ResourceKind { get; }
        public ShaderStages ShaderStages { get; }
        public uint ResourceSet { get; }
    }
}
