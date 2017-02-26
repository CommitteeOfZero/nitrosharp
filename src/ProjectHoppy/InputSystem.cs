using SciAdvNet.MediaLayer.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectHoppy
{
    public class InputSystem
    {
        private bool _wasDown = false;
        private readonly Game _game;

        public InputSystem(Game game)
        {
            _game = game;
        }

        public void Update()
        {
            if (Mouse.IsButtonDown(MouseButton.Left) && !_wasDown)
            {
                _wasDown = true;
                _game.StopInteracting();
            }
        }
    }
}
