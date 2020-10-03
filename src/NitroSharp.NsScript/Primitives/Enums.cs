namespace NitroSharp.NsScript
{
    public enum MouseState
    {
        Invalid,
        Normal,
        Over,
        Down,
        Clicked,
        Leave
    }

    public enum NsEntityAction
    {
        Lock,
        Unlock,
        DestroyWhenIdle,
        Disable,
        Enable,
        Start,
        Stop,

        EnableFiltering,
        SetAdditiveBlend,
        SetReverseSubtractiveBlend,
        SetMultiplicativeBlend,

        Play,
        Pause,
        Resume,

        NoTypewriterAnimation,

        Other,
    }

    public enum NsEaseFunction
    {
        Linear = 0,
        QuadraticEaseIn,
        CubicEaseIn,
        QuarticEaseIn,
        QuadraticEaseOut,
        CubicEaseOut,
        QuarticEaseOut,
        SineEaseIn,
        SineEaseOut,
        SineEaseInOut,
        SineEaseOutIn
    }

    public enum NsAudioKind
    {
        BackgroundMusic,
        SoundEffect,
        Voice
    }

    public enum NsVoiceAction
    {
        Play,
        Stop
    }

    public enum NsScrollDirection
    {
        Vertical,
        Horizontal
    }

    public enum NsOutlineOffset
    {
        Unspecified,
        Center,
        Left,
        Top,
        TopLeft,
        TopRight,
        Right,
        BottomLeft,
        BottomRight,
        Bottom
    }

    public enum NsFocusDirection
    {
        Left,
        Up,
        Right,
        Down
    }
}
