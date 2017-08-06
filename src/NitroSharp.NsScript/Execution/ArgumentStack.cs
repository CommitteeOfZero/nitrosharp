using System;
using System.Collections.Generic;

namespace NitroSharp.NsScript.Execution
{
    internal sealed class ArgumentStack : Stack<ConstantValue>
    {
        public ArgumentStack(IEnumerable<ConstantValue> collection)
            : base(collection)
        {
        }

        public int PopInt() => Pop().As<int>();
        public string PopString() => Pop().As<string>();
        public bool PopBool() => Pop().As<bool>();

        public NsCoordinate PopCoordinate()
        {
            var value = Pop();
            if (value.Type == NsBuiltInType.Integer)
            {
                int i = value.As<int>();
                var origin = value.IsDeltaIntegerValue ? NsCoordinateOrigin.CurrentValue : NsCoordinateOrigin.Zero;
                return new NsCoordinate(i, origin, 0.0f);
            }
            else
            {
                return PredefinedConstants.Coordinate(value.As<string>());
            }
        }

        public NsColor PopColor()
        {
            var value = Pop();
            if (value.Type == NsBuiltInType.String)
            {
                return NsColor.FromString(value.As<string>());
            }

            return NsColor.FromRgb(value.As<int>());
        }

        public NsEntityAction PopNssAction()
        {
            string actionName = PopString();
            return PredefinedConstants.EntityAction(actionName);
        }

        public NsAudioKind PopAudioKind()
        {
            string audioKindString = PopString();
            return PredefinedConstants.AudioKind(audioKindString);
        }

        public TimeSpan PopTimeSpan()
        {
            int ms = PopInt();
            return TimeSpan.FromMilliseconds(ms);
        }

        public NsEasingFunction PopEasingFunction()
        {
            string functionName = PopString();
            return string.IsNullOrEmpty(functionName) ? NsEasingFunction.None : PredefinedConstants.EasingFunction(functionName);
        }
    }
}
