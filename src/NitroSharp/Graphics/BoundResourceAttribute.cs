using System;
using Veldrid;

namespace NitroSharp.Graphics
{
    internal sealed class BoundResourceAttribute : Attribute
    {
        public BoundResourceAttribute(ResourceKind kind, ShaderStages shaderStages)
            : this(null, kind, shaderStages)
        {
        }

        public BoundResourceAttribute(string name, ResourceKind kind, ShaderStages shaderStages)
        {
            ResourceName = name;
            ResourceKind = kind;
            ShaderStages = shaderStages;
        }

        public string ResourceName { get; }
        public ResourceKind ResourceKind { get; }
        public ShaderStages ShaderStages { get; }
    }
}
