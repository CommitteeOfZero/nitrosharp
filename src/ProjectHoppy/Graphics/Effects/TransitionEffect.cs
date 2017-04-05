using SharpDX.Direct2D1;
using System;
using System.IO;
using SharpDX.Mathematics.Interop;
using SharpDX;
using System.Runtime.InteropServices;

namespace ProjectHoppy.Graphics.Effects
{
    [CustomEffect("Nigga", "ProjectHoppy", "SomeAnonDev")]
    [CustomEffectInput("Texture")]
    [CustomEffectInput("Mask")]
    public class TransitionEffect : CustomEffectBase, DrawTransform
    {
        private static readonly Guid EffectGuid = Guid.NewGuid(); //new Guid("ebbd02e0-755f-45ac-a1a8-d6bb729c4e46");
        private DrawInformation _drawInformation;
        private EffectConstantBuffer _constants;

        public int InputCount => 2;

        [PropertyBinding(0, "0.0", "1.0", "0.0")]
        public float Opacity
        {
            get => _constants.Opacity;
            set
            {
                if (_constants.Opacity != value)
                {
                    _constants.Opacity = MathUtil.Clamp(value, 0.0f, 1.0f);
                    UpdateConstants();
                }
            }
        }

        [PropertyBinding(1, "0.0", "1.0", "0.1")]
        public float Feather
        {
            get => _constants.Feather;
            set
            {
                _constants.Feather = MathUtil.Clamp(value, 0.0f, 1.0f);
                UpdateConstants();
            }
        }

        public override void Initialize(EffectContext effectContext, TransformGraph transformGraph)
        {
            var bytecode = File.ReadAllBytes("Shaders/Transition.bin");
            effectContext.LoadPixelShader(EffectGuid, bytecode);
            transformGraph.SetSingleTransformNode(this);
        }

        public RawRectangle MapInputRectanglesToOutputRectangle(RawRectangle[] inputRects, RawRectangle[] inputOpaqueSubRects, out RawRectangle outputOpaqueSubRect)
        {
            outputOpaqueSubRect = default(Rectangle);
            return inputRects[0];
        }

        public RawRectangle MapInvalidRect(int inputIndex, RawRectangle invalidInputRect)
        {
            return invalidInputRect;
        }

        public void MapOutputRectangleToInputRectangles(RawRectangle outputRect, RawRectangle[] inputRects)
        {
            for (int i = 0; i < InputCount; i++)
            {
                inputRects[i] = outputRect;
            }
        }

        public void SetDrawInformation(DrawInformation drawInfo)
        {
            _drawInformation = drawInfo;

            drawInfo.SetPixelShader(EffectGuid, PixelOptions.None);
            drawInfo.SetInputDescription(0, new InputDescription(Filter.Anisotropic, 1));
            drawInfo.SetInputDescription(1, new InputDescription(Filter.Anisotropic, 1));
        }

        private void UpdateConstants()
        {
            _drawInformation?.SetPixelConstantBuffer(ref _constants);
        }

        public override void PrepareForRender(ChangeType changeType)
        {
            UpdateConstants();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EffectConstantBuffer
        {
            public float Opacity;
            public float Feather;
        }
    }
}
