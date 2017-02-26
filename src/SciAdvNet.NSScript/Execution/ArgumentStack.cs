using System;
using System.Collections.Generic;

namespace SciAdvNet.NSScript.Execution
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

        public NssCoordinate PopCoordinate()
        {
            var value = Pop();
            if (value.Type == NssType.Integer)
            {
                NssRelativePosition relativeTo = value.IsRelative == true ? NssRelativePosition.Current : NssRelativePosition.Zero;
                return new NssCoordinate(value.As<int>(), relativeTo);
            }
            else
            {
                var relativeTo = PredefinedConstants.Positions[value.As<string>()];
                return new NssCoordinate(0, relativeTo);
            }
        }

        public NssColor PopColor()
        {
            var value = Pop();
            return value.Type == NssType.Integer ? NssColor.FromRgb(value.As<int>()) : PredefinedConstants.Colors[value.As<string>()];
        }

        public TimeSpan PopTimeSpan()
        {
            int ms = PopInt();
            return TimeSpan.FromMilliseconds(ms);
        }
    }
}
