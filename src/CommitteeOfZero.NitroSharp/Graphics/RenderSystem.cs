using System.Collections.Generic;
using System.Linq;
using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Graphics;
using System;
using System.Numerics;

namespace CommitteeOfZero.NitroSharp.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        private INitroRenderer _renderer;
        private Texture2D _secondaryRenderTarget;
        private Queue<TextVisual> _textVisuals;

        public RenderSystem(DxRenderContext renderContext)
        {
            RenderContext = renderContext;
            _textVisuals = new Queue<TextVisual>();
        }

        public DxRenderContext RenderContext { get; }
        private System.Drawing.Size DesignResolution => RenderContext.Window.DesiredSize;

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
            Vector2 scaleFactor = RenderContext.Window.ScaleFactor;

            _renderer.Target = _secondaryRenderTarget;
            using (RenderContext.NewDrawingSession(RgbaValueF.Black, present: true))
            {
                base.Update(deltaMilliseconds);

                _renderer.Target = _renderer.PrimaryRenderTarget;
                _renderer.SetTransform(Matrix3x2.CreateScale(scaleFactor));
                _renderer.Draw(_secondaryRenderTarget);

                while (_textVisuals.Count > 0)
                {
                    var text = _textVisuals.Dequeue();
                    RenderItem(text, scaleFactor);
                }
            }
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
                if (visual is TextVisual text)
                {
                    _textVisuals.Enqueue(text);
                    return;
                }

                RenderItem(visual, Vector2.One);
            }
        }

        private void RenderItem(Visual visual, Vector2 scale)
        {
            var transform = visual.Entity.Transform.GetWorldMatrix(DesignResolution) * Matrix3x2.CreateScale(scale);
            _renderer.SetTransform(transform);
            visual.Render(_renderer);
        }

        public void LoadCommonResources()
        {
            _renderer = new DxNitroRenderer(RenderContext, DesignResolution);
            _secondaryRenderTarget = _renderer.CreateRenderTarget(DesignResolution);
        }

        public void Dispose()
        {
            _secondaryRenderTarget.Dispose();
            _renderer.Dispose();
        }
    }
}
