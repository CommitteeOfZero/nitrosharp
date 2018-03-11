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

        private readonly GraphicsDevice _gd;
        private readonly SpriteEffect _spriteEffect;
        private readonly FillEffect _fillEffect;
        private CommandList _cl;

        private Vertex2D[] _vertices;
        private int _offset;
        private DeviceBuffer _vertexBuffer;
        private readonly DeviceBuffer _indexBuffer;

        private readonly Stack<Matrix3x2> _transforms = new Stack<Matrix3x2>();

        public Canvas(GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;
            _spriteEffect = EffectLibrary.LoadEffect<SpriteEffect>(_gd);
            _spriteEffect.Sampler = _gd.LinearSampler;
            _fillEffect = EffectLibrary.LoadEffect<FillEffect>(_gd);

            _vertices = new Vertex2D[InitialVertexBufferCapacity];
            CreateVertexBuffer(InitialVertexBufferCapacity);
            _indexBuffer = graphicsDevice.CreateStaticBuffer(new ushort[] { 0, 1, 2, 2, 1, 3 }, BufferUsage.IndexBuffer);
        }

        private void CreateVertexBuffer(uint vertexCount)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(Vertex2D.SizeInBytes * vertexCount, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        public void Begin(CommandList cl, in Viewport viewport)
        {
            _cl = cl;
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

            _spriteEffect.SetOrthographicsProjection(viewport);
            _fillEffect.SetOrthographicsProjection(viewport);

            _offset = 0;
        }

        public void SetTransform(in Matrix3x2 transform)
        {
            _transforms.Push(transform);
        }

        private Matrix3x2 PopTransform()
        {
            if (_transforms.Count > 0)
            {
                return _transforms.Pop();
            }

            return Matrix3x2.Identity;
        }

        public void FillRectangle(float x, float y, float width, float height, in RgbaFloat fillColor)
            => FillRectangle(new RectangleF(x, y, width, height), fillColor);

        public void FillRectangle(in RectangleF rect, in RgbaFloat fillColor)
        {
            DrawQuadGeometry(rect, fillColor);

            _fillEffect.Begin(_cl);
            _fillEffect.SetTransform(PopTransform());
            _fillEffect.End();

            Submit();
        }

        public void DrawImage(BindableTexture image, float x, float y, float opacity = 1.0f)
            => DrawImage(image, new RectangleF(x, y, image.Width, image.Height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(BindableTexture image, float x, float y, in RgbaFloat color)
            => DrawImage(image, new RectangleF(x, y, image.Width, image.Height), color);

        public void DrawImage(BindableTexture image, float x, float y, float width, float height, float opacity = 1.0f)
            =>  DrawImage(image, null, new RectangleF(x, y, width, height), new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(BindableTexture image, in RectangleF rect, float opacity = 1.0f)
            => DrawImage(image, rect, new RgbaFloat(1.0f, 1.0f, 1.0f, opacity));

        public void DrawImage(BindableTexture image, in RectangleF rect, in RgbaFloat color)
            => DrawImage(image, null, rect, color);

        public void DrawImage(BindableTexture image, in RectangleF? sourceRect, in RectangleF destinationRect, in RgbaFloat color)
        {
            var sourceRectangle = sourceRect ?? new RectangleF(0, 0, (int)image.Width, (int)image.Height);

            var texCoordTL = new Vector2(
                sourceRectangle.Left / image.Width,
                sourceRectangle.Top / image.Height);

            var texCoordBR = new Vector2(
                sourceRectangle.Right / image.Width,
                sourceRectangle.Bottom / image.Height);

            DrawQuadGeometry(destinationRect, color, texCoordTL, texCoordBR);

            _spriteEffect.Begin(_cl);
            _spriteEffect.SetTransform(PopTransform());
            _spriteEffect.Texture = image.GetTextureView();
            _spriteEffect.End();

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

            ref var VertexTL = ref _vertices[_offset];
            VertexTL.Position.X = rect.X;
            VertexTL.Position.Y = rect.Y;
            VertexTL.Color = color;
            VertexTL.TexCoords.X = texCoordTL.X;
            VertexTL.TexCoords.Y = texCoordTL.Y;

            ref var VertexTR = ref _vertices[_offset + 1];
            VertexTR.Position.X = rect.X + rect.Width;
            VertexTR.Position.Y = rect.Y;
            VertexTR.Color = color;
            VertexTR.TexCoords.X = texCoordBR.X;
            VertexTR.TexCoords.Y = texCoordTL.Y;

            ref var VertexBL = ref _vertices[_offset + 2];
            VertexBL.Position.X = rect.X;
            VertexBL.Position.Y = rect.Y + rect.Height;
            VertexBL.Color = color;
            VertexBL.TexCoords.X = texCoordTL.X;
            VertexBL.TexCoords.Y = texCoordBR.Y;

            ref var VertexBR = ref _vertices[_offset + 3];
            VertexBR.Position.X = rect.X + rect.Width;
            VertexBR.Position.Y = rect.Y + rect.Height;
            VertexBR.Color = color;
            VertexBR.TexCoords.X = texCoordBR.X;
            VertexBR.TexCoords.Y = texCoordBR.Y;

            _offset += 4;
        }

        public void DrawQuadGeometry(in RectangleF rect, in RgbaFloat color)
        {
            EnsureCapacity();

            ref var VertexTL = ref _vertices[_offset];
            VertexTL.Position.X = rect.X;
            VertexTL.Position.Y = rect.Y;
            VertexTL.Color = color;
            VertexTL.TexCoords.X = 0;
            VertexTL.TexCoords.Y = 0;

            ref var VertexTR = ref _vertices[_offset + 1];
            VertexTR.Position.X = rect.X + rect.Width;
            VertexTR.Position.Y = rect.Y;
            VertexTR.Color = color;
            VertexTR.TexCoords.X = 1;
            VertexTR.TexCoords.Y = 0;

            ref var VertexBL = ref _vertices[_offset + 2];
            VertexBL.Position.X = rect.X;
            VertexBL.Position.Y = rect.Y + rect.Height;
            VertexBL.Color = color;
            VertexBL.TexCoords.X = 0;
            VertexBL.TexCoords.Y = 1;

            ref var VertexBR = ref _vertices[_offset + 3];
            VertexBR.Position.X = rect.X + rect.Width;
            VertexBR.Position.Y = rect.Y + rect.Height;
            VertexBR.Color = color;
            VertexBR.TexCoords.X = 1;
            VertexBR.TexCoords.Y = 1;

            _offset += 4;
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
