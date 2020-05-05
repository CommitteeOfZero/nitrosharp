using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using Veldrid;

namespace NitroSharp
{
    internal sealed class InputHandler
    {
        public static void HandleInput(Context context)
        {
            InputContext input = context.Input;
            if (input.IsMouseButtonDownThisFrame(MouseButton.Left)
                || input.IsKeyDownThisFrame(Key.Enter)
                || input.IsKeyDownThisFrame(Key.KeypadEnter)
                || input.IsKeyDownThisFrame(Key.Space))
            {
                if (context.VM.MainThread is ThreadContext { IsActive: false } mainThread)
                {
                    context.VM.ResumeThread(mainThread);
                }
            }

            SystemVariableLookup sys = context.VM.SystemVariables;
            if (input.IsMouseButtonDown(MouseButton.Right))
            {
                set(ref sys.RightButtonDown);
            }

            static void set(ref ConstantValue value) => value = ConstantValue.True;
        }
    }
}
