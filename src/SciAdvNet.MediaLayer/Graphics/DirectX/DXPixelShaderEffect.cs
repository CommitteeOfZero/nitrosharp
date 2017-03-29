using System;
using System.Collections.Generic;
using System.Text;
using SharpDX.Direct2D1;

namespace SciAdvNet.MediaLayer.Graphics.DirectX
{
    public class DXPixelShaderEffect : SharpDX.Direct2D1.CustomEffectBase
    {
        public override void PrepareForRender(ChangeType changeType)
        {
            base.PrepareForRender(changeType);
        }

        public override void SetGraph(TransformGraph transformGraph)
        {
            base.SetGraph(transformGraph);
        }
    }
}
