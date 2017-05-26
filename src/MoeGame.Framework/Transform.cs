using System.Collections.Generic;
using System.Numerics;

namespace MoeGame.Framework
{
    public class Transform : Component
    {
        private Transform _parent;
        private List<Transform> _children;

        private Vector2 _localPosition = Vector2.Zero;
        private Vector2 _localScale = Vector2.One;

        public Transform Parent
        {
            get => _parent;
            set => SetParent(value);
        }

        public IReadOnlyList<Transform> Children => ChildrenList;
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

        public Vector2 Position
        {
            get => Parent == null ? _localPosition : Vector2.Transform(_localPosition, Parent.GetWorldMatrix());
            set => _localPosition = Parent == null ? value : value - Parent.Position;
        }

        public Vector2 LocalPosition
        {
            get => _localPosition;
            set => _localPosition = value;
        }

        public Vector2 Scale
        {
            get => Parent == null ? _localScale : _localScale * Parent.Scale;
            set => _localScale = Parent == null ? value : value / Parent.Scale;
        }

        public Vector2 LocalScale
        {
            get => _localScale;
            set => _localScale = value;
        }

        public Matrix3x2 GetWorldMatrix()
        {
            var matrix = Matrix3x2.CreateScale(LocalScale) * Matrix3x2.CreateTranslation(LocalPosition);
            if (Parent != null)
            {
                matrix *= Parent.GetWorldMatrix();
            }

            return matrix;
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
