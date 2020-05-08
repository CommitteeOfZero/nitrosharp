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
                BuiltInConstant.Pause => NsEntityAction.Pause,
                BuiltInConstant.Resume => NsEntityAction.Resume,
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

        public static NsOutlineOffset ToOutlineOffset(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant.Around => NsOutlineOffset.Center,
                BuiltInConstant.Left => NsOutlineOffset.Left,
                BuiltInConstant.Up => NsOutlineOffset.Top,
                BuiltInConstant.LeftUp => NsOutlineOffset.TopLeft,
                BuiltInConstant.RightUp => NsOutlineOffset.TopRight,
                BuiltInConstant.Right => NsOutlineOffset.Right,
                BuiltInConstant.LeftDown => NsOutlineOffset.BottomLeft,
                BuiltInConstant.RightDown => NsOutlineOffset.BottomRight,
                BuiltInConstant.Down => NsOutlineOffset.Bottom,
                _ =>  ThrowHelper.UnexpectedValue<NsOutlineOffset>()
            };
        }

        public static NsScrollbarKind ToScrollbarKind(BuiltInConstant val)
        {
            return val switch
            {
                BuiltInConstant.Vertical => NsScrollbarKind.Vertical,
                BuiltInConstant.Horizon => NsScrollbarKind.Horizontal,
                _ => ThrowHelper.UnexpectedValue<NsScrollbarKind>()
            };
        }
    }
}
