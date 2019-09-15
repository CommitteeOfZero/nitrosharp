using NitroSharp.Graphics;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Primitives;
using System;
using System.Numerics;
using Veldrid;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        private void SetParent(Entity entity, Entity parent)
        {
            var table = _world.GetTable<EntityTable>(entity);
            table.Parents.Set(entity, parent);
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
            try
            {
                var texId = new AssetId(fileName);
                _ = Content.GetTexture(texId, increaseRefCount: false);
                RgbaFloat white = RgbaFloat.White;
                _world.CreateSprite(entityName, texId, default, 0, default, ref white);
            }
            catch (ContentLoadException)
            {
            }
        }

        public override void CreateSprite(
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
                CreateSpriteCore(entityName, fileOrExistingEntityName, x, y, priority);
            }
        }

        private void CaptureFramebuffer(string entityName, NsCoordinate x, NsCoordinate y, int priority)
        {
        }

        public override void CreateSpriteEx(
            string entityName, int priority,
            NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY,
            int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            CreateSpriteCore(entityName, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void CreateSpriteCore(
            string entityName, string fileOrExistingEntityName,
            NsCoordinate x, NsCoordinate y,
            int priority, RectangleF? srcRect = null)
        {
            _logger.LogInformation($"Loading sprite: {fileOrExistingEntityName}");

            string source = fileOrExistingEntityName;
            if (source.ToUpperInvariant().Contains("COLOR")) { return; }
            if (_world.TryGetEntity(fileOrExistingEntityName, out Entity existingEnitity))
            {
                source = _world.Sprites.ImageSources.GetValue(existingEnitity).Image.NormalizedPath;
            }

            var texId = new AssetId(source);
            Texture texture;
            try
            {
                texture = Content.GetTexture(texId, increaseRefCount: false);
            }
            catch (ContentLoadException)
            {
                return;
            }
            var texSize = new Size(texture.Width, texture.Height);

            RgbaFloat color = RgbaFloat.White;
            var bounds = new Vector2(texSize.Width, texSize.Height);
            var sourceRectangle = srcRect ?? new RectangleF(0, 0, bounds.X, bounds.Y);
            var size = new SizeF(sourceRectangle.Width, sourceRectangle.Height);

            Entity entity = _world.CreateSprite(entityName, texId, sourceRectangle, priority, size, ref color);
            SetPosition(entity, x, y);
            Entity parent = _world.Sprites.Parents.GetValue(entity);
            if (parent.IsValid && parent.Kind == EntityKind.Choice)
            {
                var parsedName = new EntityName(entityName);
                ChoiceTable choices = _world.Choices;
                switch (parsedName.MouseState)
                {
                    case Interactivity.MouseState.Normal:
                        choices.MouseUsualSprite.Set(parent, entity);
                        break;
                    case Interactivity.MouseState.Over:
                        choices.MouseOverSprite.Set(parent, entity);
                        break;
                    case Interactivity.MouseState.Pressed:
                        choices.MouseClickSprite.Set(parent, entity);
                        break;
                }
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

        public override void DrawTransition(string sourceEntityName, TimeSpan duration, NsRational initialOpacity, NsRational finalOpacity, NsRational feather, NsEasingFunction easingFunction, string maskFileName, TimeSpan delay)
        {
            Interpreter.SuspendThread(CurrentThread, duration);
        }

        public override int GetWidth(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                return (int)_world.GetTable<RenderItemTable>(entity).Bounds.GetValue(entity).Width;
            }

            return 0;
        }

        public override int GetHeight(string entityName)
        {
            if (_world.TryGetEntity(entityName, out Entity entity))
            {
                return (int)_world.GetTable<RenderItemTable>(entity).Bounds.GetValue(entity).Height;
            }

            return 0;
        }

        internal void SetPosition(Entity entity, NsCoordinate x, NsCoordinate y)
        {
            var parentBounds = new SizeF(1280, 720);

            RenderItemTable properties = _world.GetTable<RenderItemTable>(entity);

            ref TransformComponents transform = ref properties.TransformComponents.Mutate(entity);
            SizeF bounds = properties.Bounds.GetValue(entity);

            Entity parent = properties.Parents.GetValue(entity);
            if (parent.IsValid && parent.IsVisual)
            {
                parentBounds = _world.GetTable<RenderItemTable>(parent).Bounds.GetValue(parent);
            }

            var value = new Vector2(
                x.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.X + x.Value : x.Value,
                y.Origin == NsCoordinateOrigin.CurrentValue ? transform.Position.Y + y.Value : y.Value);

            var anchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);
            Vector2 translateOrigin;
            translateOrigin.X = x.Origin switch
            {
                NsCoordinateOrigin.Left => 0.0f,
                NsCoordinateOrigin.Center => 0.5f,
                NsCoordinateOrigin.Right => 1.0f,
                _ => 0.0f
            };
            translateOrigin.Y = y.Origin switch
            {
                NsCoordinateOrigin.Top => 0.0f,
                NsCoordinateOrigin.Center => 0.5f,
                NsCoordinateOrigin.Bottom => 1.0f,
                _ => 0.0f
            };

            Vector2 position = translateOrigin * new Vector2(parentBounds.Width, parentBounds.Height);
            position -= anchorPoint * new Vector2(bounds.Width, bounds.Height);
            position += value;

            transform.Position = new Vector3(position.X, position.Y, 0);
        }
    }
}
