using CommitteeOfZero.Nitro.Graphics.Visuals;
using CommitteeOfZero.NsScript;
using MoeGame.Framework;
using MoeGame.Framework.Content;
using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace CommitteeOfZero.Nitro
{
    public sealed partial class NitroCore
    {
        private System.Drawing.Size _viewport;

        public override void AddRectangle(string entityName, int priority, NsCoordinate x, NsCoordinate y, int width, int height, NsColor color)
        {
            var rect = new RectangleVisual
            {
                Position = Position(x, y, Vector2.Zero, width, height),
                Width = width,
                Height = height,
                Color = new RgbaValueF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, 1.0f),
                Priority = priority
            };

            _entities.Create(entityName, replace: true).WithComponent(rect);
        }

        public override void LoadImage(string entityName, string fileName)
        {
            CurrentThread.Suspend();
            _content.LoadAsync<TextureAsset>(fileName).ContinueWith(t =>
            {
                var visual = new TextureVisual
                {
                    AssetRef = fileName,
                    IsEnabled = false,
                    Width = t.Result.Width,
                    Height = t.Result.Height
                };

                _entities.Create(entityName, replace: true).WithComponent(visual);
                CurrentThread.Resume();
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, _game.MainLoopTaskScheduler);
        }

        public override void AddTexture(string entityName, int priority, NsCoordinate x, NsCoordinate y, string fileOrExistingEntityName)
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
            var position = Position(x, y, Vector2.Zero, _viewport.Width, _viewport.Height);
            var screencap = new ScreenCap
            {
                Position = position,
                Priority = priority,
            };

            _entities.Create(entityName, replace: true).WithComponent(screencap);
        }

        public override void AddClippedTexture(string entityName, int priority, NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY, int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            AddTextureCore(entityName, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void AddTextureCore(string entityName, string fileOrExistingEntityName, NsCoordinate x, NsCoordinate y, int priority, RectangleF? srcRect = null)
        {
            Visual parentVisual = null;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                if (_entities.TryGet(parentEntityName, out var parentEntity))
                {
                    parentVisual = parentEntity.GetComponent<Visual>();
                }
            }

            var texture = new TextureVisual
            {
                ParentVisual = parentVisual,
                Priority = priority,
                SourceRectangle = srcRect
            };

            if (srcRect != null)
            {
                texture.Width = srcRect.Value.Width;
                texture.Height = srcRect.Value.Height;
            }

            if (_entities.TryGet(fileOrExistingEntityName, out var existingEnitity))
            {
                var existingTexture = existingEnitity.GetComponent<TextureVisual>();
                if (existingTexture != null)
                {
                    var position = Position(x, y, Vector2.Zero, (int)existingTexture.Width, (int)existingTexture.Height);
                    texture.AssetRef = existingTexture.AssetRef;
                    texture.Position = position;

                    if (srcRect == null)
                    {
                        texture.Width = existingTexture.Width;
                        texture.Height = existingTexture.Height;
                    }

                    _entities.Create(entityName, replace: true).WithComponent(texture);
                }
            }
            else
            {
                CurrentThread.Suspend();
                _content.LoadAsync<TextureAsset>(fileOrExistingEntityName).ContinueWith(t =>
                {
                    var asset = t.Result;
                    var position = Position(x, y, Vector2.Zero, (int)asset.Width, (int)asset.Height);

                    texture.AssetRef = fileOrExistingEntityName;
                    texture.Position = position;

                    if (srcRect == null)
                    {
                        texture.Width = asset.Width;
                        texture.Height = asset.Height;
                    }

                    _entities.Create(entityName, replace: true).WithComponent(texture);
                    CurrentThread.Resume();

                }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, _game.MainLoopTaskScheduler);
            }
        }

        public override int GetTextureWidth(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var texture = entity.GetComponent<TextureVisual>();
                if (texture != null)
                {
                    return (int)texture.Width;
                }
            }

            return 0;
        }

        private Vector2 Position(NsCoordinate x, NsCoordinate y, Vector2 current, int width, int height)
        {
            float absoluteX = NssToAbsoluteCoordinate(x, current.X, width, _viewport.Width);
            float absoluteY = NssToAbsoluteCoordinate(y, current.Y, height, _viewport.Height);

            return new Vector2(absoluteX, absoluteY);
        }

        private static float NssToAbsoluteCoordinate(NsCoordinate coordinate, float currentValue, float objectDimension, float viewportDimension)
        {
            switch (coordinate.Origin)
            {
                case NsCoordinateOrigin.Zero:
                    return coordinate.Value;

                case NsCoordinateOrigin.CurrentValue:
                    return coordinate.Value + currentValue;

                case NsCoordinateOrigin.Left:
                    return coordinate.Value - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Top:
                    return coordinate.Value - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Right:
                    return viewportDimension - objectDimension * coordinate.AnchorPoint;
                case NsCoordinateOrigin.Bottom:
                    return viewportDimension - objectDimension * coordinate.AnchorPoint;

                case NsCoordinateOrigin.Center:
                    return (viewportDimension - objectDimension) / 2.0f;

                default:
                    throw new ArgumentException("Illegal value.", nameof(coordinate));
            }
        }
    }
}
