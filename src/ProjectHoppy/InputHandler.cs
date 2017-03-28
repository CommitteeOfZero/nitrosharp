using SciAdvNet.MediaLayer.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProjectHoppy
{
    public class InputHandler : System
    {
        private readonly N2SystemImplementation _n2system;

        public InputHandler(N2SystemImplementation n2system)
        {
            _n2system = n2system;
        }

        public override void Update(float deltaMilliseconds)
        {
            if (Mouse.IsButtonDownThisFrame(MouseButton.Left))
            {
                _n2system.Interpreter.ResumeThread(_n2system.CallingThreadId);
            }
        }
    }
}
