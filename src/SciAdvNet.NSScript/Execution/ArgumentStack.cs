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
                NssPositionOrigin relativeTo = value.IsDelta == true ? NssPositionOrigin.Current : NssPositionOrigin.Zero;
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
            if (value.Type == NssType.String)
            {
                string s = value.As<string>();

                string strNum = s.Substring(1);
                if (int.TryParse(strNum, out int num))
                {
                    return NssColor.FromRgb(num);
                }
                else
                {
                    return PredefinedConstants.Colors[value.As<string>()];
                }

            }
            return NssColor.FromRgb(value.As<int>());
        }

        public NssEntityAction PopNssAction()
        {
            string strAction = PopString();
            return PredefinedConstants.Actions.TryGetValue(strAction, out NssEntityAction action) ? action : NssEntityAction.Other;
        }

        public TimeSpan PopTimeSpan()
        {
            int ms = PopInt();
            return TimeSpan.FromMilliseconds(ms);
        }

        public EasingFunction PopEasingFunction()
        {
            string str = PopString();
            return string.IsNullOrEmpty(str) ? EasingFunction.None : PredefinedConstants.EasingFunctions[str];
        }
    }
}
