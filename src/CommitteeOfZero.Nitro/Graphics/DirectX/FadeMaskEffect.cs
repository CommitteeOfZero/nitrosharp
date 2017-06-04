using SharpDX.Direct2D1;
using System;
using System.IO;
using SharpDX.Mathematics.Interop;
using SharpDX;
using System.Runtime.InteropServices;

namespace CommitteeOfZero.Nitro.Graphics
{
    [CustomEffect("FadeMask", "CommitteeOfZero.Nitro", "CommitteeOfZero")]
    [CustomEffectInput("Texture")]
    [CustomEffectInput("Mask")]
    public class FadeMaskEffect : CustomEffectBase, DrawTransform
    {
        private EffectContext _context;
        private TransformGraph _transformGraph;

        private static readonly Guid EffectGuid = Guid.NewGuid();
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
            _context = effectContext;
            _transformGraph = transformGraph;

            var bytecode = File.ReadAllBytes("Shaders/FadeMask.bin");
            effectContext.LoadPixelShader(EffectGuid, bytecode);
            transformGraph.SetSingleTransformNode(this);
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            _transformGraph.Dispose();

            base.Dispose(disposing);
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
            inputRects[0] = outputRect;
            inputRects[1] = new RawRectangle(0, 0, outputRect.Right - outputRect.Left, outputRect.Bottom - outputRect.Top);
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
