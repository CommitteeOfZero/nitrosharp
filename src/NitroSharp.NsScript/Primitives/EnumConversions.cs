namespace NitroSharp.NsScript
{
    internal static class EnumConversions
    {
        public static NsEntityAction ToEntityAction(BuiltInConstant enumValue)
        {
            return enumValue switch
            {
                BuiltInConstant.Lock => NsEntityAction.Lock,
                BuiltInConstant.Unlock => NsEntityAction.Unlock,
                BuiltInConstant.Play => NsEntityAction.Play,
                BuiltInConstant.Disused => NsEntityAction.Dispose,
                BuiltInConstant.Erase => NsEntityAction.ResetText,
                BuiltInConstant.Hideable => NsEntityAction.Hide,
                BuiltInConstant.Start => NsEntityAction.Start,
                BuiltInConstant.Stop => NsEntityAction.Stop,
                BuiltInConstant.AddRender => NsEntityAction.SetAdditiveBlend,
                BuiltInConstant.SubRender => NsEntityAction.SetReverseSubtractiveBlend,
                BuiltInConstant.MulRender => NsEntityAction.SetMultiplicativeBlend,
                BuiltInConstant.Smoothing => NsEntityAction.UseLinearFiltering,
                _ => NsEntityAction.Other,
            };
        }

        public static NsEasingFunction ToEasingFunction(BuiltInConstant enumValue)
        {
            return enumValue switch
            {
                BuiltInConstant._None => NsEasingFunction.None,

                BuiltInConstant.Axl1 => NsEasingFunction.QuadraticEaseIn,
                BuiltInConstant.Axl2 => NsEasingFunction.CubicEaseIn,
                BuiltInConstant.Axl3 => NsEasingFunction.QuarticEaseIn,

                BuiltInConstant.Dxl1 => NsEasingFunction.QuadraticEaseOut,
                BuiltInConstant.Dxl2 => NsEasingFunction.CubicEaseOut,
                BuiltInConstant.Dxl3 => NsEasingFunction.QuarticEaseOut,

                BuiltInConstant.AxlAuto => NsEasingFunction.SineEaseIn,
                BuiltInConstant.DxlAuto => NsEasingFunction.SineEaseOut,
                BuiltInConstant.AxlDxl => NsEasingFunction.SineEaseInOut,
                BuiltInConstant.DxlAxl => NsEasingFunction.SineEaseOutIn,

                _ => throw ThrowHelper.UnexpectedValue(nameof(enumValue)),
            };
        }

        public static NsAudioKind ToAudioKind(BuiltInConstant enumValue)
        {
            return enumValue switch
            {
                BuiltInConstant.BGM => NsAudioKind.BackgroundMusic,
                BuiltInConstant.SE => NsAudioKind.SoundEffect,
                BuiltInConstant.Voice => NsAudioKind.Voice,
                _ => throw ThrowHelper.UnexpectedValue(nameof(enumValue)),
            };
        }
    }
}
