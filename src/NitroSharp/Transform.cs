using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal class Transform : Component
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

        public Vector3 Dimensions => new Vector3(Entity.Visual.Bounds.ToVector(), 0.0f);

        /// <summary>
        /// Position (in pixels) relative to the parent.
        /// </summary>
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;

        public Matrix4x4 GetTransformMatrix()
        {
            var matrix = Matrix4x4.CreateScale(Scale, new Vector3(0.5f) * Dimensions)
                * Matrix4x4.CreateTranslation(Position)
                * Matrix4x4.CreateFromYawPitchRoll(
                    MathUtil.ToRadians(Rotation.Y),
                    MathUtil.ToRadians(Rotation.X),
                    MathUtil.ToRadians(Rotation.Z));

            if (Parent != null)
            {
                matrix *= Parent.GetTransformMatrix();
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

        public override void OnRemoved()
        {
            if (Parent != null && Parent.Children.Any())
            {
                Parent.ChildrenList.Remove(this);
            }

            Parent = null;
        }
    }
}
