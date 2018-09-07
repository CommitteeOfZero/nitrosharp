using NitroSharp.Graphics;
using NitroSharp.NsScript;
using NitroSharp.Primitives;
using NitroSharp.Text;
using System;
using System.Numerics;
using Veldrid;

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private void SetParent(Entity entity, Entity parent)
        {
            var table = _world.GetTable<EntityTable>(entity);
            table.Parents.Set(entity, parent);
        }

        public override void CreateText(string entityName, int priority, NsCoordinate x, NsCoordinate y, string text)
        {
            var fontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
            TextLayout layout = new TextLayout(fontFamily, new Size(300, 50), 256);
            layout.Append(new TextRun()
            {
                Text = text,
                Color = RgbaFloat.White
            });

            RgbaFloat color = RgbaFloat.White;
            Entity entity = _world.CreateTextInstance(entityName, layout, priority, ref color);
            SetPosition(entity, x, y);
        }

        public override void FillRectangle(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height, NsColor color)
        {
            RgbaFloat rgba = color.ToRgbaFloat();
            Entity e = _world.CreateRectangle(entityName, priority, new SizeF(width, height), ref rgba);
            SetPosition(e, x, y);
        }

        public override void LoadImage(string entityName, string fileName)
        {
            Content.Get<BindableTexture>(fileName);
            RgbaFloat white = RgbaFloat.White;
            _world.CreateSprite(entityName, fileName, default, 0, default, ref white);
        }

        public override void AddTexture(
            string entityName, int priority,
            NsCoordinate x, NsCoordinate y,
            string fileOrExistingEntityName)
        {
            if (fileOrExistingEntityName.Equals("SCREEN", StringComparison.OrdinalIgnoreCase))
            {
                CaptureFramebuffer(entityName, x, y, priority);
            }
            else
            {
                AddTextureCore(entityName, fileOrExistingEntityName, x, y, priority);
            }
        }

        private void CaptureFramebuffer(string entityName, NsCoordinate x, NsCoordinate y, int priority)
        {
        }

        public override void AddClippedTexture(
            string entityName, int priority,
            NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY,
            int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            AddTextureCore(entityName, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void AddTextureCore(
            string entityName, string fileOrExistingEntityName,
            NsCoordinate x, NsCoordinate y,
            int priority, RectangleF? srcRect = null)
        {
            Entity parentEntity = default;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                _world.TryGetEntity(parentEntityName, out parentEntity);
            }

            string source = fileOrExistingEntityName;
            if (_world.TryGetEntity(fileOrExistingEntityName, out Entity existingEnitity))
            {
                source = _world.Sprites.SpriteComponents.GetValue(existingEnitity).Image;
            }

            var texture = Content.Get<BindableTexture>(source);

            RgbaFloat color = RgbaFloat.White;
            var bounds = new Vector2(texture.Asset.Width, texture.Asset.Height);
            var sourceRectangle = srcRect.HasValue
                ? srcRect.Value
                : new RectangleF(0, 0, bounds.X, bounds.Y);
            var size = new SizeF(sourceRectangle.Width, sourceRectangle.Height);

            Entity entity = _world.CreateSprite(entityName, source, sourceRectangle, priority, size, ref color);
            SetPosition(entity, x, y);
            if (parentEntity.IsValid)
            {
                SetParent(entity, parentEntity);
            }
        }

        public override void CreateCube(
            string entityName, int priority,
            string front, string back,
            string right, string left,
            string top, string bottom)
        {
            //var cube = new Cube(
            //    Content.Get<BindableTexture>(front),
            //    Content.Get<BindableTexture>(back),
            //    Content.Get<BindableTexture>(left),
            //    Content.Get<BindableTexture>(right),
            //    Content.Get<BindableTexture>(top),
            //    Content.Get<BindableTexture>(bottom));

            //cube.Priority = priority;
            //var EntityHandle = _world.Create(entityName).WithComponent(cube);
            //EntityHandle.Transform.TransformationOrder = TransformationOrder.ScaleTranslationRotation;
        }

        public override int GetWidth(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                return (int)_world.GetTable<Visuals>(entity).Bounds.GetValue(entity).Width;
            }

            return 0;
        }

        public override int GetHeight(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                return (int)_world.GetTable<Visuals>(entity).Bounds.GetValue(entity).Height;
            }

            return 0;
        }

        internal void SetPosition(Entity entity, NsCoordinate x, NsCoordinate y)
        {
            SizeF parentBounds = new SizeF(1280, 720);

            Visuals properties = _world.GetTable<Visuals>(entity);

            ref TransformComponents transform = ref properties.TransformComponents.Mutate(entity);
            SizeF bounds = properties.Bounds.GetValue(entity);

            Entity parent = properties.Parents.GetValue(entity);
            if (parent.IsValid)
            {
                parentBounds = _world.GetTable<Visuals>(parent).Bounds.GetValue(parent);
            }

            var value = new Vector2(
                x.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.X + x.Value : x.Value,
                y.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.Y + y.Value : y.Value);

            var anchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);

            Vector2 translateOrigin;
            switch (x.Origin)
            {
                case NsCoordinateOrigin.Left:
                default:
                    translateOrigin.X = 0.0f;
                    break;
                case NsCoordinateOrigin.Center:
                    translateOrigin.X = 0.5f;
                    break;
                case NsCoordinateOrigin.Right:
                    translateOrigin.X = 1.0f;
                    break;
            }

            switch (y.Origin)
            {
                case NsCoordinateOrigin.Top:
                default:
                    translateOrigin.Y = 0.0f;
                    break;
                case NsCoordinateOrigin.Center:
                    translateOrigin.Y = 0.5f;
                    break;
                case NsCoordinateOrigin.Bottom:
                    translateOrigin.Y = 1.0f;
                    break;
            }

            Vector2 position = translateOrigin * new Vector2(parentBounds.Width, parentBounds.Height);
            position -= anchorPoint * new Vector2(bounds.Width, bounds.Height);
            position += value;

            transform.Position = new Vector3(position.X, position.Y, 0);
        }
    }
}
