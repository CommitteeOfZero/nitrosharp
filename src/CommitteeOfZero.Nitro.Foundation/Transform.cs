using CommitteeOfZero.Nitro.Foundation.Graphics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace CommitteeOfZero.Nitro.Foundation
{
    public class Transform : Component
    {
        private Transform _parent;
        private List<Transform> _children;

        public Transform Parent
        {
            get => _parent;
            set => SetParent(value);
        }

        public IEnumerable<Transform> Children => _children ?? Enumerable.Empty<Transform>();
        private List<Transform> ChildrenList
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<Transform>();
                }

                return _children;
            }
        }

        public SizeF Bounds
        {
            get
            {
                var visual = Entity.GetComponent<VisualBase>();
                return visual == null ? SizeF.Empty : visual.Measure();
            }
        }

        public Vector2 TranslateOrigin { get; set; }
        public Vector2 ScaleOrigin { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 AnchorPoint { get; set; }
        public Vector2 Margin { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;

        public void SetTranslateOriginX(float value) => TranslateOrigin = new Vector2(value, TranslateOrigin.Y);
        public void SetTranslateOriginY(float value) => TranslateOrigin = new Vector2(TranslateOrigin.X, value);
        public void SetAnchorPointX(float value) => AnchorPoint = new Vector2(value, AnchorPoint.Y);
        public void SetAnchorPointY(float value) => AnchorPoint = new Vector2(AnchorPoint.X, value);
        public void SetMarginX(float value) => Margin = new Vector2(value, Margin.Y);
        public void SetMarginY(float value) => Margin = new Vector2(Margin.X, value);

        public Matrix3x2 GetWorldMatrix(SizeF screenBounds)
        {
            var parentBounds = Parent == null ? screenBounds : Parent.Bounds;
            var relativePosition = CalculateRelativePosition(parentBounds, Margin);

            var matrix = Matrix3x2.CreateScale(Scale, new Vector2(ScaleOrigin.X * Bounds.Width, ScaleOrigin.Y * Bounds.Height))
                * Matrix3x2.CreateTranslation(relativePosition);

            if (Parent != null)
            {
                matrix *= Parent.GetWorldMatrix(screenBounds);
            }

            return matrix;
        }

        private Vector2 CalculateRelativePosition(SizeF parentBounds, Vector2 margin)
        {
            var v = TranslateOrigin * new Vector2(parentBounds.Width, parentBounds.Height);
            v -= AnchorPoint * new Vector2(Bounds.Width, Bounds.Height);
            v += margin;

            return v;
        }

        private void SetParent(Transform newParent)
        {
            var oldParent = _parent;
            if (newParent != oldParent)
            {
                oldParent?.ChildrenList?.Remove(this);
                newParent?.ChildrenList?.Add(this);
                _parent = newParent;
            }
        }
    }
}
