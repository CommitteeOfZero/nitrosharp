using System.Collections.Generic;
using System.Linq;
using NitroSharp.Foundation;
using NitroSharp.Foundation.Graphics;
using System;
using System.Numerics;
using System.Drawing;

namespace NitroSharp.Graphics
{
    public sealed class RenderSystem : EntityProcessingSystem, IDisposable
    {
        internal INitroRenderer _renderer;
        private Texture2D _secondaryRenderTarget;
        private Texture2D _screen;
        private Queue<TextVisual> _textVisuals;

        public RenderSystem(DxRenderContext renderContext)
        {
            RenderContext = renderContext;
            _textVisuals = new Queue<TextVisual>();
        }

        public DxRenderContext RenderContext { get; }
        private System.Drawing.Size DesignResolution => RenderContext.Window.DesiredSize;
        private SizeF ActualResolution => _renderer.BackBuffer.Size;

        protected override void DeclareInterests(ISet<Type> interests)
        {
            interests.Add(typeof(Visual));
        }

        public override void OnRelevantEntityAdded(Entity entity)
        {
            var screencap = entity.GetComponent<Screenshot>();
            if (screencap != null)
            {
                _screen.CopyFrom(_renderer.BackBuffer);
            }
        }

        public override void OnRelevantEntityRemoved(Entity entity)
        {
            var visual = entity.GetComponent<Visual>();
            visual.Free(_renderer);
        }

        public override void Update(float deltaMilliseconds)
        {
            Vector2 scaleFactor = new Vector2(ActualResolution.Width / DesignResolution.Width, ActualResolution.Height / DesignResolution.Height);

            _renderer.Target = _secondaryRenderTarget;
            using (RenderContext.NewDrawingSession(RgbaValueF.Black, present: true))
            {
                base.Update(deltaMilliseconds);

                _renderer.Target = _renderer.BackBuffer;
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

            if (visual is Screenshot)
            {
                _renderer.Draw(_screen, new System.Drawing.RectangleF(0, 0, DesignResolution.Width, DesignResolution.Height));
                return;
            }

            visual.Render(_renderer);
        }

        public void LoadCommonResources()
        {
            _renderer = new DxNitroRenderer(RenderContext, DesignResolution, new[] { "Fonts" });
            _secondaryRenderTarget = _renderer.CreateRenderTarget(DesignResolution);
            _screen = _renderer.CreateRenderTarget(ActualResolution);

            _renderer.BackBufferResized += OnBackBufferResized;
        }

        private void OnBackBufferResized(object sender, EventArgs e)
        {
            _screen.Dispose();
            _screen = _renderer.CreateRenderTarget(ActualResolution);
        }

        public void Dispose()
        {
            _renderer.Dispose();
            _secondaryRenderTarget.Dispose();
        }
    }
}
