using System;
using System.Collections.Generic;
using System.Numerics;
using NitroSharp.Primitives;
using Veldrid;

namespace NitroSharp.Graphics
{
    public sealed class Canvas : IDisposable
    {
        private const uint InitialVertexBufferCapacity = 256 * 4;
        private readonly SharedEffectProperties2D _props;
        private readonly GraphicsDevice _gd;
        private readonly SpriteEffect _spriteEffect;
        private readonly FillEffect _fillEffect;
        private EffectPipelineState _pipelineState;

        private CommandList _cl;
        private Vertex2D[] _vertices;
        private int _offset;
        private DeviceBuffer _vertexBuffer;
        private readonly DeviceBuffer _indexBuffer;
        private readonly Stack<Matrix4x4> _transforms = new Stack<Matrix4x4>();

        public Canvas(
            GraphicsDevice graphicsDevice,
            EffectLibrary effectLibrary,
            SharedEffectProperties2D sharedEffectProperties)
        {
            _props = sharedEffectProperties;
            _gd = graphicsDevice;
            _spriteEffect = effectLibrary.Get<SpriteEffect>(sharedEffectProperties);
            _spriteEffect.Properties.Sampler = _gd.Aniso4xSampler;
            _fillEffect = effectLibrary.Get<FillEffect>(sharedEffectProperties);

            _vertices = new Vertex2D[InitialVertexBufferCapacity];
            CreateVertexBuffer(InitialVertexBufferCapacity);
            var indices = new ushort[] { 0, 1, 2, 2, 1, 3 };
            _indexBuffer = _gd.CreateStaticBuffer(indices, BufferUsage.IndexBuffer);
        }

        private void CreateVertexBuffer(uint vertexCount)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(
                    Vertex2D.SizeInBytes * vertexCount,
                    BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        public void Begin(CommandList cl, Framebuffer renderTarget = null)
        {
            _cl = cl;
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

            _offset = 0;
        }

        public void SetTransform(in Matrix4x4 transform)
        {
            _transforms.Push(transform);
        }

        private Matrix4x4 PopTransform()
        {
            if (_transforms.Count > 0)
            {
                return _transforms.Pop();
            }

            return Matrix4x4.Identity;
        }

        public void FillRectangle(float x, float y, float width, float height, in RgbaFloat fillColor)
            => FillRectangle(new RectangleF(x, y, width, height), fillColor);

        public void FillRectangle(in RectangleF rect, in RgbaFloat fillColor)
        {
            DrawQuadGeometry(rect, fillColor);

            var properties = _fillEffect.Properties;
            properties.BeginRecording(_cl);
            properties.Transform = PopTransform();
            properties.EndRecording();

            _fillEffect.Apply(_cl);

            Submit();
        }

        public void DrawImage(TextureView image, float x, float y, float opacity = 1.0f)
            => DrawImage(image, new RectangleF(x, y, image.Target.Width, image.Target.Height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(TextureView image, float x, float y, in RgbaFloat color)
            => DrawImage(image, new RectangleF(x, y, image.Target.Width, image.Target.Height), color);

        public void DrawImage(TextureView image, float x, float y, float width, float height, float opacity = 1.0f)
            =>  DrawImage(image, null, new RectangleF(x, y, width, height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(TextureView image, in RectangleF rect, float opacity = 1.0f)
            => DrawImage(image, rect, new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(TextureView image, in RectangleF rect, in RgbaFloat color)
            => DrawImage(image, null, rect, color);

        public void DrawImage(TextureView image, in RectangleF? sourceRect, in RectangleF destinationRect, in RgbaFloat color)
        {
            var sourceRectangle = sourceRect ?? new RectangleF(0, 0, (int)image.Target.Width, (int)image.Target.Height);

            var texCoordTL = new Vector2(
                sourceRectangle.Left / image.Target.Width,
                sourceRectangle.Top / image.Target.Height);

            var texCoordBR = new Vector2(
                sourceRectangle.Right / image.Target.Width,
                sourceRectangle.Bottom / image.Target.Height);

            DrawQuadGeometry(destinationRect, color, texCoordTL, texCoordBR);

            var properties = _spriteEffect.Properties;
            properties.BeginRecording(_cl);
            properties.Transform = PopTransform();
            properties.Texture = image;
            properties.EndRecording();

            _spriteEffect.Apply(_cl);

            Submit();
        }

        private void Submit()
        {
            _cl.DrawIndexed(6, 1, 0, _offset - 4, 0);
        }

        public void End()
        {
            _gd.UpdateBuffer(_vertexBuffer, 0, _vertices);
        }

        public void Dispose()
        {
            _indexBuffer.Dispose();
            _vertexBuffer.Dispose();
            _spriteEffect.Dispose();
            _fillEffect.Dispose();
        }

        public void DrawQuadGeometry(in RectangleF rect, in RgbaFloat color, in Vector2 texCoordTL, in Vector2 texCoordBR)
        {
            EnsureCapacity();
            int offset = _offset;

            ref var VertexTL = ref _vertices[offset];
            VertexTL.Position.X = rect.X;
            VertexTL.Position.Y = rect.Y;
            VertexTL.Color = color;
            VertexTL.TexCoords.X = texCoordTL.X;
            VertexTL.TexCoords.Y = texCoordTL.Y;

            ref var VertexTR = ref _vertices[++offset];
            VertexTR.Position.X = rect.X + rect.Width;
            VertexTR.Position.Y = rect.Y;
            VertexTR.Color = color;
            VertexTR.TexCoords.X = texCoordBR.X;
            VertexTR.TexCoords.Y = texCoordTL.Y;

            ref var VertexBL = ref _vertices[++offset];
            VertexBL.Position.X = rect.X;
            VertexBL.Position.Y = rect.Y + rect.Height;
            VertexBL.Color = color;
            VertexBL.TexCoords.X = texCoordTL.X;
            VertexBL.TexCoords.Y = texCoordBR.Y;

            ref var VertexBR = ref _vertices[++offset];
            VertexBR.Position.X = rect.X + rect.Width;
            VertexBR.Position.Y = rect.Y + rect.Height;
            VertexBR.Color = color;
            VertexBR.TexCoords.X = texCoordBR.X;
            VertexBR.TexCoords.Y = texCoordBR.Y;

            _offset = ++offset;
        }

        public void DrawQuadGeometry(in RectangleF rect, in RgbaFloat color)
        {
            EnsureCapacity();
            int offset = _offset;

            ref var VertexTL = ref _vertices[offset];
            VertexTL.Position.X = rect.X;
            VertexTL.Position.Y = rect.Y;
            VertexTL.Color = color;
            VertexTL.TexCoords.X = 0;
            VertexTL.TexCoords.Y = 0;

            ref var VertexTR = ref _vertices[++offset];
            VertexTR.Position.X = rect.X + rect.Width;
            VertexTR.Position.Y = rect.Y;
            VertexTR.Color = color;
            VertexTR.TexCoords.X = 1;
            VertexTR.TexCoords.Y = 0;

            ref var VertexBL = ref _vertices[++offset];
            VertexBL.Position.X = rect.X;
            VertexBL.Position.Y = rect.Y + rect.Height;
            VertexBL.Color = color;
            VertexBL.TexCoords.X = 0;
            VertexBL.TexCoords.Y = 1;

            ref var VertexBR = ref _vertices[++offset];
            VertexBR.Position.X = rect.X + rect.Width;
            VertexBR.Position.Y = rect.Y + rect.Height;
            VertexBR.Color = color;
            VertexBR.TexCoords.X = 1;
            VertexBR.TexCoords.Y = 1;

            _offset = ++offset;
        }

        private void EnsureCapacity()
        {
            if (_offset + 4 >= _vertices.Length)
            {
                int newSize = _vertices.Length * 2;
                CreateVertexBuffer((uint)newSize);
                Array.Resize(ref _vertices, newSize);
            }
        }
    }
}
