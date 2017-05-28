using CommitteeOfZero.Nitro.Graphics;
using CommitteeOfZero.NsScript;
using CommitteeOfZero.Nitro.Foundation;
using CommitteeOfZero.Nitro.Foundation.Content;
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
            var rgba = new RgbaValueF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, 1.0f);
            var rect = new RectangleVisual(width, height, rgba, 1.0f, priority);

            var entity = _entities.Create(entityName, replace: true)
                .WithComponent(rect)
                .WithPosition(x, y);
        }

        public override void LoadImage(string entityName, string fileName)
        {
            //var sprite = new Sprite(fileName);
            //_entities.Create(entityName, replace: true).WithComponent(sprite);

            Interpreter.Suspend();
            _content.LoadOnThreadPool<TextureAsset>(fileName).ContinueWith(t =>
            {
                var sprite = new Sprite(fileName, null, 1.0f, 0);
                _entities.Create(entityName, replace: true).WithComponent(sprite);
                Interpreter.Resume();
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
            var screencap = new ScreenshotVisual
            {
                Priority = priority,
            };

            _entities.Create(entityName, replace: true)
                .WithComponent(screencap)
                .WithPosition(x, y);
        }

        public override void AddClippedTexture(string entityName, int priority, NsCoordinate dstX, NsCoordinate dstY,
            NsCoordinate srcX, NsCoordinate srcY, int width, int height, string srcEntityName)
        {
            var srcRectangle = new RectangleF(srcX.Value, srcY.Value, width, height);
            AddTextureCore(entityName, srcEntityName, dstX, dstY, priority, srcRectangle);
        }

        private void AddTextureCore(string entityName, string fileOrExistingEntityName, NsCoordinate x, NsCoordinate y, int priority, RectangleF? srcRect = null)
        {
            Entity parentEntity = null;
            int idxSlash = entityName.IndexOf('/');
            if (idxSlash > 0)
            {
                string parentEntityName = entityName.Substring(0, idxSlash);
                _entities.TryGet(parentEntityName, out parentEntity);
            }

            if (_entities.TryGet(fileOrExistingEntityName, out var existingEnitity))
            {
                var existingTexture = existingEnitity.GetComponent<Sprite>();
                if (existingTexture != null)
                {

                    var texture = new Sprite(existingTexture.Source, srcRect, 1.0f, priority);
                    _entities.Create(entityName, replace: true)
                        .WithComponent(texture)
                        .WithParent(parentEntity)
                        .WithPosition(x, y);
                }
            }
            else
            {
                Interpreter.Suspend();
                _content.LoadOnThreadPool<TextureAsset>(fileOrExistingEntityName).ContinueWith(t =>
                {
                    var asset = t.Result;
                    var texture = new Sprite(fileOrExistingEntityName, srcRect, 1.0f, priority);

                    _entities.Create(entityName, replace: true)
                        .WithComponent(texture)
                        .WithParent(parentEntity)
                        .WithPosition(x, y);

                    Interpreter.Resume();

                }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, _game.MainLoopTaskScheduler);
            }
        }

        public override int GetTextureWidth(string entityName)
        {
            if (_entities.TryGet(entityName, out var entity))
            {
                var texture = entity.GetComponent<Sprite>();
                if (texture != null)
                {
                    return 0;
                }
            }

            return 0;
        }

        internal static void SetPosition(Transform transform, NsCoordinate x, NsCoordinate y)
        {
            transform.SetMarginX(x.Origin == NsCoordinateOrigin.CurrentValue ? transform.Margin.X + x.Value : x.Value);
            transform.AnchorPoint = new Vector2(x.AnchorPoint, y.AnchorPoint);

            switch (x.Origin)
            {
                case NsCoordinateOrigin.Left:
                default:
                    transform.SetTranslateOriginX(0.0f);
                    break;

                case NsCoordinateOrigin.Center:
                    transform.SetTranslateOriginX(0.5f);
                    break;

                case NsCoordinateOrigin.Right:
                    transform.SetTranslateOriginX(1.0f);
                    break;
            }

            transform.SetMarginY(y.Origin == NsCoordinateOrigin.CurrentValue ? transform.Margin.Y + y.Value : y.Value);
            switch (y.Origin)
            {
                case NsCoordinateOrigin.Top:
                default:
                    transform.SetTranslateOriginY(0.0f);
                    break;

                case NsCoordinateOrigin.Center:
                    transform.SetTranslateOriginY(0.5f);
                    break;

                case NsCoordinateOrigin.Bottom:
                    transform.SetTranslateOriginY(1.0f);
                    break;
            }
        }
    }
}
