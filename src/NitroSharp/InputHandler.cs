using NitroSharp.NsScript;
using NitroSharp.NsScript.VM;
using Veldrid;

namespace NitroSharp
{
    internal sealed class InputHandler
    {
        public static void ProcessInput(Context context)
        {
            InputContext input = context.Input;
            SystemVariableLookup sys = context.VM.SystemVariables;
            if (input.IsMouseButtonDown(MouseButton.Right))
            {
                set(ref sys.RightButtonDown);
            }

            static void set(ref ConstantValue value) => value = ConstantValue.True;
        }
    }
}
