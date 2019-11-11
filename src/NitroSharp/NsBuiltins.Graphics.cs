using NitroSharp.Graphics;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Primitives;
using System;
using System.Numerics;
using Veldrid;
using NitroSharp.Experimental;

#nullable enable

namespace NitroSharp
{
    internal sealed partial class NsBuiltins
    {
        public override void BoxBlur(string entityName, uint nbPasses)
        {
        }

        public override void Grayscale(string entityQuery)
        {
            foreach ((Entity entity, _) in _world.Query(entityQuery))
            {
                var storage = _world.GetStorage<RenderItem2DStorage>(entity);
                storage.CommonProperties.GetRef(entity).Effect = EffectKind.Grayscale;
            }
        }

        public override void CreateRectangle(
            string name, int priority,
            NsCoordinate x, NsCoordinate y,
            int width, int height, NsColor color)
        {
            (Entity e, _) = _world.Rectangles.Uninitialized.New(
                new EntityName(name),
                new SizeF(width, height),
                priority,
                color.ToRgbaFloat()
            );
            SetPosition(e, x, y);
        }

        public override void LoadImage(string entityName, string fileName)
        {
            var textureId = new AssetId(fileName);
            if (Content.RequestTexture(textureId, out _, incrementRefCount: false))
            {
                _world.Images.Uninitialized.New(
                    new EntityName(entityName),
                    new ImageSource(textureId, sourceRectangle: default)
                );
            }
        }

        public override void CreateSprite(
            string name, int priority,
            NsCoordinate x, NsCoordinate y,
            string fileOrExistingEntityName)
        {
            if (fileOrExistingEntityName.Equals("SCREEN", StringComparison.OrdinalIgnoreCase))
            {
                CaptureFramebuffer(name, x, y, priority);
            }
            else
            {
                CreateSpriteCore(name, fileOrExistingEntityName, x, y, priority);
            }
        }

        private void CaptureFramebuffer(string entityName, NsCoordinate x, NsCoordinate y, int priority)
        {
        }

        public override void CreateSpriteEx(
            string name, int priority,
            NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY,
            int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            CreateSpriteCore(name, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void CreateSpriteCore(
            string name, string fileOrExistingEntityName,
            NsCoordinate x, NsCoordinate y,
            int priority, RectangleF? srcRect = null)
        {
            _logger.LogInformation($"Loading sprite: {fileOrExistingEntityName}");

            string source = fileOrExistingEntityName;
            if (source.ToUpperInvariant().Contains("COLOR")) { return; }
            if (_world.TryGetEntity(new EntityName(source), out Entity existingEnitity))
            {
                var storage = _world.GetStorage<AbstractImageStorage>(existingEnitity);
                source = storage.ImageSources.GetRef(existingEnitity).ImageId.NormalizedPath;
            }

            var textureId = new AssetId(source);
            if (!Content.RequestTexture(textureId, out Size texSize))
            {
                return;
            }

            var sourceRectangle = srcRect ?? new RectangleF(Vector2.Zero, texSize);
            var localBounds = new SizeF(sourceRectangle.Width, sourceRectangle.Height);
            (Entity entity, _) = _world.Sprites.Uninitialized.New(
                new EntityName(name),
                priority,
                new ImageSource(textureId, sourceRectangle),
                RgbaFloat.White,
                localBounds
            );
            SetPosition(entity, x, y);
            //Entity parent = _world.Sprites.Parents.GetRef(entity);
            //if (parent.IsValid && parent.Kind == EntityKind.Choice)
            //{
            //    var parsedName = new EntityName(entityName);
            //    ChoiceTable choices = _world.Choices;
            //    switch (parsedName.MouseState)
            //    {
            //        case MouseState.Normal:
            //            choices.MouseUsualSprite.Set(parent, entity);
            //            break;
            //        case MouseState.Over:
            //            choices.MouseOverSprite.Set(parent, entity);
            //            break;
            //        case MouseState.Pressed:
            //            choices.MouseClickSprite.Set(parent, entity);
            //            break;
            //    }
            //}
        }

        public override void CreateCube(
            string name, int priority,
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

        public override void DrawTransition(
            string sourceEntityName,
            TimeSpan duration,
            NsRational initialOpacity,
            NsRational finalOpacity,
            NsRational feather,
            NsEasingFunction easingFunction,
            string maskFileName,
            TimeSpan delay)
        {
            Content.RequestTexture(new AssetId(maskFileName), out _);

            Interpreter.SuspendThread(CurrentThread, duration);
        }

        public override int GetWidth(string entityName)
        {
            if (_world.TryGetEntity(new EntityName(entityName), out Entity entity))
            {
                var storage = _world.GetStorage<RenderItem2DStorage>(entity);
                return (int)storage.LocalBounds.GetRef(entity).Width;
            }

            return 0;
        }

        public override int GetHeight(string entityName)
        {
            if (_world.TryGetEntity(new EntityName(entityName), out Entity entity))
            {
                var storage = _world.GetStorage<RenderItem2DStorage>(entity);
                return (int)storage.LocalBounds.GetRef(entity).Height;
            }

            return 0;
        }

        internal void SetPosition(Entity entity, NsCoordinate x, NsCoordinate y)
        {
            var parentBounds = new SizeF(1280, 720);

            var storage = _world.GetStorage<RenderItem2DStorage>(entity);
            ref TransformComponents transform = ref storage.TransformComponents.GetRef(entity);
            SizeF bounds = storage.LocalBounds.GetRef(entity);

            Entity parent = _world.GetParent(entity);
            if (parent.IsValid)
            {
                parentBounds = _world.GetStorage<RenderItem2DStorage>(parent)
                    .LocalBounds.GetRef(parent);
            }

            var value = new Vector2(
                x.Origin == NsCoordinateOrigin.CurrentValue
                    ? transform.Position.X + x.Value
                    : x.Value,
                y.Origin == NsCoordinateOrigin.CurrentValue
                    ? transform.Position.Y + y.Value
                    : y.Value
            );

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

            Vector2 position = translateOrigin * parentBounds.ToVector();
            position -= anchorPoint * new Vector2(bounds.Width, bounds.Height);
            position += value;

            transform.Position = new Vector3(position.X, position.Y, 0);
        }
    }
}
