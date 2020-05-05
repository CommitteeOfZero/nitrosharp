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
                BuiltInConstant.PushText => NsEntityAction.NoTypewriterAnimation,
                _ => NsEntityAction.Other,
            };
        }

        public static NsEaseFunction ToEaseFunction(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant._None => NsEaseFunction.None,

                BuiltInConstant.Axl1 => NsEaseFunction.QuadraticEaseIn,
                BuiltInConstant.Axl2 => NsEaseFunction.CubicEaseIn,
                BuiltInConstant.Axl3 => NsEaseFunction.QuarticEaseIn,

                BuiltInConstant.Dxl1 => NsEaseFunction.QuadraticEaseOut,
                BuiltInConstant.Dxl2 => NsEaseFunction.CubicEaseOut,
                BuiltInConstant.Dxl3 => NsEaseFunction.QuarticEaseOut,

                BuiltInConstant.AxlAuto => NsEaseFunction.SineEaseIn,
                BuiltInConstant.DxlAuto => NsEaseFunction.SineEaseOut,
                BuiltInConstant.AxlDxl => NsEaseFunction.SineEaseInOut,
                BuiltInConstant.DxlAxl => NsEaseFunction.SineEaseOutIn,

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
