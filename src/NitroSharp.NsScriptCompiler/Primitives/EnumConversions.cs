namespace NitroSharp.NsScript
{
    internal static class EnumConversions
    {
        public static NsEntityAction ToEntityAction(BuiltInConstant enumValue)
        {
            switch (enumValue)
            {
                case BuiltInConstant.Lock:
                    return NsEntityAction.Lock;
                case BuiltInConstant.Unlock:
                    return NsEntityAction.Unlock;
                case BuiltInConstant.Play:
                    return NsEntityAction.Play;
                case BuiltInConstant.Disused:
                    return NsEntityAction.Dispose;
                case BuiltInConstant.Erase:
                    return NsEntityAction.ResetText;
                case BuiltInConstant.Hideable:
                    return NsEntityAction.Hide;
                case BuiltInConstant.Start:
                    return NsEntityAction.Start;
                case BuiltInConstant.Stop:
                    return NsEntityAction.Stop;

                default:
                    return NsEntityAction.Other;
            }
        }

        public static NsEasingFunction ToEasingFunction(BuiltInConstant enumValue)
        {
            switch (enumValue)
            {
                case BuiltInConstant._None:
                    return NsEasingFunction.None;

                case BuiltInConstant.Axl1:
                    return NsEasingFunction.QuadraticEaseIn;
                case BuiltInConstant.Axl2:
                    return NsEasingFunction.CubicEaseIn;
                case BuiltInConstant.Axl3:
                    return NsEasingFunction.QuarticEaseIn;

                case BuiltInConstant.Dxl1:
                    return NsEasingFunction.QuadraticEaseOut;
                case BuiltInConstant.Dxl2:
                    return NsEasingFunction.CubicEaseOut;
                case BuiltInConstant.Dxl3:
                    return NsEasingFunction.QuarticEaseOut;

                case BuiltInConstant.AxlAuto:
                    return NsEasingFunction.SineEaseIn;
                case BuiltInConstant.DxlAuto:
                    return NsEasingFunction.SineEaseOut;
                case BuiltInConstant.AxlDxl:
                    return NsEasingFunction.SineEaseInOut;
                case BuiltInConstant.DxlAxl:
                    return NsEasingFunction.SineEaseOutIn;

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(enumValue));
            }
        }

        public static NsAudioKind ToAudioKind(BuiltInConstant enumValue)
        {
            switch (enumValue)
            {
                case BuiltInConstant.BGM:
                    return NsAudioKind.BackgroundMusic;
                case BuiltInConstant.SE:
                    return NsAudioKind.SoundEffect;
                case BuiltInConstant.Voice:
                    return NsAudioKind.Voice;

                default:
                    throw ThrowHelper.UnexpectedValue(nameof(enumValue));
            }
        }
    }
}
