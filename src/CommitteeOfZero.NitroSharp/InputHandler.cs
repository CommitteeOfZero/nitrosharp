﻿using CommitteeOfZero.NitroSharp.Graphics;
using CommitteeOfZero.NsScript.Execution;
using CommitteeOfZero.NitroSharp.Foundation;
using CommitteeOfZero.NitroSharp.Foundation.Input;
using System;

namespace CommitteeOfZero.NitroSharp
{
    public sealed class InputHandler : GameSystem
    {
        private readonly NitroCore _nitroCore;

        public InputHandler(NitroCore nitroCore)
        {
            _nitroCore = nitroCore;
        }

        public override void Update(float deltaMilliseconds)
        {
            if (ShouldAdvance() && !TrySkipAnimation())
            {
                if (_nitroCore.Interpreter.Status != InterpreterStatus.Suspended && _nitroCore.MainThread.SleepTimeout == TimeSpan.MaxValue)
                {
                    _nitroCore.MainThread.Resume();
                }
            }

            if (Keyboard.IsKeyDownThisFrame(Key.R))
            {
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            }
        }

        private bool TrySkipAnimation()
        {
            var text = _nitroCore.TextEntity;
            if (text != null)
            {
                var animation = text.GetComponent<SmoothTextAnimation>();
                if (animation?.Progress < 1.0f)
                {
                    animation.Stop();
                    text.RemoveComponent(animation);
                    text.AddComponent(new TextSkipAnimation());
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldAdvance()
        {
            return Mouse.IsButtonDownThisFrame(MouseButton.Left) || Keyboard.IsKeyDownThisFrame(Key.Space)
                || Keyboard.IsKeyDownThisFrame(Key.Enter) || Keyboard.IsKeyDownThisFrame(Key.KeypadEnter);
        }
    }
}