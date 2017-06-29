using System.Collections.Generic;
using System.Linq;
using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Graphics;
using System;
using System.Numerics;
using SharpDX.Direct2D1;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private INitroRenderer _renderer;

        private System.Drawing.Size DesignResolution => new System.Drawing.Size(RenderContext.Window.Width, RenderContext.Window.Height);

        public RenderSystem(DxRenderContext renderContext)
        {
            RenderContext = renderContext;
        }

        public DxRenderContext RenderContext { get; }

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var screencap = entity.GetComponent<Screenshot>();
            screencap?.Take(_renderer);
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            var visual = entity.GetComponent<Visual>();
            visual.Free(_renderer);
        }

        public override void Update(float deltaMilliseconds)
        {
            using (RenderContext.NewDrawingSession(RgbaValueF.Black))
            {
                base.Update(deltaMilliseconds);
            }

            //var bmp = RenderContext.DeviceContext.Target;
            //_shit = bmp;
            //RenderContext.DeviceContext.Target = _defaultTarget;
            //using (RenderContext.NewDrawingSession(RgbaValueF.Black))
            //{
            //    RenderContext.DeviceContext.Transform = SharpDX.Matrix3x2.Scaling(1.5f, 1.5f);
            //    RenderContext.DeviceContext.DrawImage(bmp);

            //    if (_text != null)
            //    {
            //        var transform = _text.Entity.Transform.GetWorldMatrix(new System.Drawing.SizeF(1280, 720));
            //        transform *= Matrix3x2.CreateScale(RenderContext.BackBufferSize.Width / 1280.0f, RenderContext.BackBufferSize.Height / 720.0f);

            //        _renderer.SetTransform(transform);
            //        _text.Render(_renderer);
            //    }
            //}

            //_text = null;
        }

        public override IEnumerable<Entity> SortEntities(IEnumerable<Entity> entities)
        {
            return entities.OrderBy(x => x.GetComponent<Visual>().Priority).ThenBy(x => x.CreationTime);
        }

        public override void Process(Entity entity, float deltaMilliseconds)
        {
            var visual = entity.GetComponent<Visual>();
            if (visual.IsEnabled)
            {
                var transform = entity.Transform.GetWorldMatrix(DesignResolution);
                _renderer.SetTransform(transform);
                visual.Render(_renderer);
            }
        }

        public void LoadCommonResources()
        {
            _renderer = new DxNitroRenderer(RenderContext, DesignResolution);
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }
}
