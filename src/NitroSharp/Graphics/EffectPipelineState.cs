using System;
using NitroSharp.Utilities;
using Veldrid;

namespace NitroSharp.Graphics
{
    public readonly struct EffectPipelineState : IEquatable<EffectPipelineState>
    {
        public readonly RasterizerStateDescription RasterizerState;
        public readonly DepthStencilStateDescription DepthStencilState;
        public readonly BlendStateDescription BlendState;
        public readonly OutputDescription OutputDescription;

        public EffectPipelineState(
            in RasterizerStateDescription rasterizerState,
            in DepthStencilStateDescription depthStencilState,
            in BlendStateDescription blendState,
            in OutputDescription outputDescription)
        {
            RasterizerState = rasterizerState;
            DepthStencilState = depthStencilState;
            BlendState = blendState;

            OutputDescription = outputDescription;
        }

        public bool Equals(EffectPipelineState other)
        {
            return RasterizerState.Equals(other.RasterizerState)
                && DepthStencilState.Equals(other.DepthStencilState)
                && BlendState.Equals(other.BlendState)
                && OutputDescription.Equals(other.OutputDescription);
        }

        public override int GetHashCode()
        {
            return HashHelper.Combine(
                RasterizerState.GetHashCode(),
                DepthStencilState.GetHashCode(),
                BlendState.GetHashCode(),
                OutputDescription.GetHashCode());
        }
    }
}
