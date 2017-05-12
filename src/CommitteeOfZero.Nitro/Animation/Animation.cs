using MoeGame.Framework;
using System;

namespace CommitteeOfZero.Nitro.Animation
{
    public abstract class Animation : Component
    {
        protected float _elapsed;

        public abstract event EventHandler Completed;
        public abstract void Advance(float deltaMilliseconds);
    }
}
