using NitroSharp.Dialogue;
using NitroSharp.Graphics;
using NitroSharp.Content;
using NitroSharp.NsScript;
using NitroSharp.NsScript.Primitives;
using NitroSharp.Primitives;
using NitroSharp.Text;
using System;
using System.Collections.Immutable;
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

        public override void CreateText(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, string text)
        {
            var fontFamily = FontService.GetFontFamily("Noto Sans CJK JP");
            var layout = new TextLayout(fontFamily, new Size(width > 0 ? (uint)width : 300, 50), 256);
            ImmutableArray<DialogueLinePart> parts = DialogueLine.Parse(text).Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] is TextPart textPart)
                {
                    layout.Append(textPart.Text, display: true);
                }
            }

            RgbaFloat color = RgbaFloat.White;
            Entity entity = _world.CreateTextInstance(entityName, layout, priority, ref color);
            _world.TextInstances.Bounds.Set(entity, new SizeF(width, 50));
            SetPosition(entity, x, y);

            Entity parentEntity = default;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                _world.TryGetEntity(parentEntityName, out parentEntity);
            }

            if (parentEntity.IsValid)
            {
                if (parentEntity.Kind == EntityKind.Choice)
                {
                    if (entityName.Contains("MouseUsual"))
                    {
                        _world.Choices.MouseUsualSprite.Set(parentEntity, entity);
                    }
                    else if (entityName.Contains("MouseOver"))
                    {
                        _world.Choices.MouseOverSprite.Set(parentEntity, entity);
                    }
                    else if (entityName.Contains("MouseClick"))
                    {
                        _world.Choices.MouseClickSprite.Set(parentEntity, entity);
                    }
                }
                else
                {
                    SetParent(entity, parentEntity);
                }
            }
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
            Entity parentEntity = default;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                _world.TryGetEntity(parentEntityName, out parentEntity);
            }

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

            RgbaFloat color = RgbaFloat.White;
            var bounds = new Vector2(texture.Width, texture.Height);
            var sourceRectangle = srcRect ?? new RectangleF(0, 0, bounds.X, bounds.Y);
            var size = new SizeF(sourceRectangle.Width, sourceRectangle.Height);

            Entity entity = _world.CreateSprite(entityName, texId, sourceRectangle, priority, size, ref color);
            SetPosition(entity, x, y);
            if (parentEntity.IsValid)
            {
                if (parentEntity.Kind == EntityKind.Choice)
                {
                    if (entityName.Contains("MouseUsual"))
                    {
                        _world.Choices.MouseUsualSprite.Set(parentEntity, entity);
                    }
                    else if (entityName.Contains("MouseOver"))
                    {
                        _world.Choices.MouseOverSprite.Set(parentEntity, entity);
                    }
                    else if (entityName.Contains("MouseClick"))
                    {
                        _world.Choices.MouseClickSprite.Set(parentEntity, entity);
                    }
                }
                else
                {
                    SetParent(entity, parentEntity);
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
            if (parent.IsValid)
            {
                parentBounds = _world.GetTable<RenderItemTable>(parent).Bounds.GetValue(parent);
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
