namespace NitroSharp.NsScript
{
    internal static class EnumConversions
    {
        public static NsEntityAction ToEntityAction(BuiltInEnumValue enumValue)
        {
            switch (enumValue)
            {
                case BuiltInEnumValue.Lock:
                    return NsEntityAction.Lock;
                case BuiltInEnumValue.Unlock:
                    return NsEntityAction.Unlock;
                case BuiltInEnumValue.Play:
                    return NsEntityAction.Play;
                case BuiltInEnumValue.Disused:
                    return NsEntityAction.Dispose;
                case BuiltInEnumValue.Erase:
                    return NsEntityAction.ResetText;
                case BuiltInEnumValue.Hideable:
                    return NsEntityAction.Hide;
                case BuiltInEnumValue.Start:
                    return NsEntityAction.Start;
                case BuiltInEnumValue.Stop:
                    return NsEntityAction.Stop;

                default:
                    return NsEntityAction.Other;
            }
        }

        public static NsEasingFunction ToEasingFunction(BuiltInEnumValue enumValue)
        {
            switch (enumValue)
            {
                case BuiltInEnumValue._None:
                    return NsEasingFunction.None;

                case BuiltInEnumValue.Axl1:
                    return NsEasingFunction.QuadraticEaseIn;
                case BuiltInEnumValue.Axl2:
                    return NsEasingFunction.CubicEaseIn;
                case BuiltInEnumValue.Axl3:
                    return NsEasingFunction.QuarticEaseIn;

                case BuiltInEnumValue.Dxl1:
                    return NsEasingFunction.QuadraticEaseOut;
                case BuiltInEnumValue.Dxl2:
                    return NsEasingFunction.CubicEaseOut;
                case BuiltInEnumValue.Dxl3:
                    return NsEasingFunction.QuarticEaseOut;

                case BuiltInEnumValue.AxlAuto:
                    return NsEasingFunction.SineEaseIn;
                case BuiltInEnumValue.DxlAuto:
                    return NsEasingFunction.SineEaseOut;
                case BuiltInEnumValue.AxlDxl:
                    return NsEasingFunction.SineEaseInOut;
                case BuiltInEnumValue.DxlAxl:
                    return NsEasingFunction.SineEaseOutIn;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(enumValue));
            }
        }

        public static NsAudioKind ToAudioKind(BuiltInEnumValue enumValue)
        {
            switch (enumValue)
            {
                case BuiltInEnumValue.BGM:
                    return NsAudioKind.BackgroundMusic;
                case BuiltInEnumValue.SE:
                    return NsAudioKind.SoundEffect;
                case BuiltInEnumValue.Voice:
                    return NsAudioKind.Voice;

                default:
                    throw ExceptionUtils.UnexpectedValue(nameof(enumValue));
            }
        }
    }
}
