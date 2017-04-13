#if WINDOWS_UWP
using MoeGame.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Windows.UI.Core;

namespace MoeGame.Framework.Platform
{
    public partial class UwpGameWindow
    {
        private void SubscribeToInputEvents()
        {
            _coreWindow.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            
            //if (Keyboard.PressedKeys.Contains())
            //Keyboard.PressedKeys
        }

        private Key UwpKeyToMlKey(VirtualKey uwpKey)
        {
            switch (uwpKey)
            {
                case VirtualKey.A:
                default:
                    return Key.A;
            }
        }
    }
}
#endif
