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
        MarkForRemoval,
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
        None = 0,
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

    public enum NsScrollbarKind
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
