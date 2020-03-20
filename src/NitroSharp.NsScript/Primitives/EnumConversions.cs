namespace NitroSharp.NsScript
{
    internal static class EnumConversions
    {
        public static NsEntityAction ToEntityAction(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant.Lock => NsEntityAction.Lock,
                BuiltInConstant.Unlock => NsEntityAction.Unlock,
                BuiltInConstant.Disused => NsEntityAction.MarkForRemoval,
                BuiltInConstant.Erase => NsEntityAction.Disable,
                BuiltInConstant.Enter => NsEntityAction.Enable,
                BuiltInConstant.Start => NsEntityAction.Start,
                BuiltInConstant.Stop => NsEntityAction.Stop,
                BuiltInConstant.Smoothing => NsEntityAction.EnableFiltering,
                BuiltInConstant.AddRender => NsEntityAction.SetAdditiveBlend,
                BuiltInConstant.SubRender => NsEntityAction.SetReverseSubtractiveBlend,
                BuiltInConstant.MulRender => NsEntityAction.SetMultiplicativeBlend,
                BuiltInConstant.Play => NsEntityAction.Play,
                _ => NsEntityAction.Other,
            };
        }

        public static NsEasingFunction ToEasingFunction(BuiltInConstant val)
        {
            return val switch
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

                _ => throw ThrowHelper.UnexpectedValue(nameof(val)),
            };
        }

        public static NsAudioKind ToAudioKind(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant.BGM => NsAudioKind.BackgroundMusic,
                BuiltInConstant.SE => NsAudioKind.SoundEffect,
                BuiltInConstant.Voice => NsAudioKind.Voice,
                _ => throw ThrowHelper.UnexpectedValue(nameof(val)),
            };
        }
    }
}
