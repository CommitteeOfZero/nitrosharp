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

        public Coordinate PopCoordinate()
        {
            var value = Pop();
            if (value.Type == NssType.Integer)
            {
                int i = value.As<int>();
                var origin = value.IsDelta.Value ? CoordinateOrigin.CurrentValue : CoordinateOrigin.Zero;
                return new Coordinate(i, origin, new Rational(0, 0));
            }
            else
            {
                return PredefinedConstants.GetCoordinate(value.As<string>());
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
