using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NitroSharp.Utilities;

namespace NitroSharp
{
    internal class OldTransform : Component
    {
        private OldTransform _parent;
        private List<OldTransform> _children;

        public OldTransform Parent
        {
            get => _parent;
            set => SetParent(value);
        }

        public IEnumerable<OldTransform> Children => _children ?? Enumerable.Empty<OldTransform>();
        private List<OldTransform> ChildrenList
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<OldTransform>();
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
        public TransformationOrder TransformationOrder;

        public Matrix4x4 GetTransformMatrix()
        {
            var center = new Vector3(0.5f) * Dimensions;
            var scale = Matrix4x4.CreateScale(Scale, center);
            var rotation = Matrix4x4.CreateRotationZ(MathUtil.ToRadians(Rotation.Z), center)
                * Matrix4x4.CreateRotationY(MathUtil.ToRadians(Rotation.Y), center)
                * Matrix4x4.CreateRotationX(MathUtil.ToRadians(Rotation.X), center);
            var translation = Matrix4x4.CreateTranslation(Position);

            var composite = TransformationOrder == TransformationOrder.ScaleRotationTranslation
                ? scale * rotation * translation
                : scale * translation * rotation;

            if (Parent != null)
            {
                composite *= Parent.GetTransformMatrix();
            }

            return composite;
        }

        private void SetParent(OldTransform newParent)
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
