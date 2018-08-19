using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NitroSharp
{
    public abstract class GameSystem
    {
        protected float DeltaTime;

        protected GameSystem()
        {
        }


        public virtual void Update(float deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }
}
