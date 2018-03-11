using NitroSharp.Graphics;
using NitroSharp.Logic.Objects;
using NitroSharp.NsScript;
using NitroSharp.Primitives;
using System;
using System.Numerics;
using Veldrid;

namespace NitroSharp
{
    internal sealed partial class CoreLogic
    {
        public override void AddRectangle(string entityName, int priority,
            NsCoordinate x, NsCoordinate y, int width, int height, NsColor color)
        {
            var rgba = new RgbaFloat(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, 1.0f);
            var rect = new RectangleVisual(width, height, rgba, 1.0f, priority);

            _entities.Create(entityName, replace: true)
                .WithComponent(rect)
                .WithPosition(x, y);
        }

        public override void LoadImage(string entityName, string fileName)
        {
            var sprite = new Sprite(_content.Get<BindableTexture>(fileName), null, 1.0f, 0);
            _entities.Create(entityName, replace: true).WithComponent(sprite);
        }

        public override void AddTexture(string entityName, int priority,
            NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName)
        {
            if (fileOrExistingEntityName.Equals("SCREEN", StringComparison.OrdinalIgnoreCase))
            {
                AddScreencap(entityName, x, y, priority);
            }
            else
            {
                AddTextureCore(entityName, fileOrExistingEntityName, x, y, priority);
            }
        }

        private void AddScreencap(string entityName, NsCoordinate x, NsCoordinate y, int priority)
        {
            //var screencap = new Screenshot
            //{
            //    Priority = priority,
            //};

            //_entities.Create(entityName, replace: true)
            //    .WithComponent(screencap)
            //    .WithPosition(x, y);
        }

        public override void AddClippedTexture(string entityName, int priority, NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY, int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            AddTextureCore(entityName, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void AddTextureCore(string entityName, string fileOrExistingEntityName,
            NsCoordinate x, NsCoordinate y, int priority, RectangleF? srcRect = null)
        {
            Entity parentEntity = null;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                _entities.TryGet(parentEntityName, out parentEntity);
            }

            string source = fileOrExistingEntityName;
            if (_entities.TryGet(fileOrExistingEntityName, out var existingEnitity))
            {
                var existingSprite = existingEnitity.GetComponent<Sprite>();
                if (existingSprite != null)
                {
                    source = existingSprite.Source.Id;
                }
            }

            var texture = new Sprite(_content.Get<BindableTexture>(source), srcRect, 1.0f, priority);
            _entities.Create(entityName, replace: true)
                .WithComponent(texture)
                .WithParent(parentEntity)
                .WithPosition(x, y);
        }

        public override int GetTextureWidth(string entityName)
        {
            return _entities.TryGet(entityName, out var entity) ? (int)entity.Transform.Dimensions.X : 0;
        }

        public override int GetTextureHeight(string entityName)
        {
            return _entities.TryGet(entityName, out var entity) ? (int)entity.Transform.Dimensions.Y : 0;
        }

        internal static void SetPosition(Transform transform, NsCoordinate x, NsCoordinate y)
        {
            var Parent = transform.Parent;
            var screenBounds = new Vector3(1280, 720, 0);
            var parentBounds = Parent == null ? screenBounds : Parent.Dimensions;

            var value = new Vector3(
                x.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.X + x.Value : x.Value,
                y.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.Y + y.Value : y.Value, 0);

            var AnchorPoint = new Vector3(x.AnchorPoint, y.AnchorPoint, 0);

            Vector3 translateOrigin;
            translateOrigin.Z = 0;
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

            var position = translateOrigin * parentBounds;
            position -= AnchorPoint * transform.Dimensions;
            position += value;

            transform.Position = position;
        }
    }
}
