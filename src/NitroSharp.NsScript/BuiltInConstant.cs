namespace NitroSharp.NsScript
{
    public enum BuiltInConstant : byte
    {
        _None,

        // Relative positions
        Center,
        Middle,
        InLeft,
        OnLeft,
        OutLeft,
        Left,
        InTop,
        OnTop,
        OutTop,
        Top,
        InRight,
        OnRight,
        OutRight,
        Right,
        InBottom,
        OnBottom,
        OutBottom,
        Bottom,

        // Used by SetNextFocus / SetFont
        Up,
        Down,
        // Used by SetFont
        LeftUp,
        RightUp,
        LeftDown,
        RightDown,
        LightDown,
        Around,
        None,

        // Colors
        Black,
        White,
        Gray,
        Red,
        Green,
        Blue,

        // Timing functions
        Axl1,
        Axl2,
        Axl3,
        Dxl1,
        Dxl2,
        Dxl3,
        AxlAuto,
        DxlAuto,
        AxlDxl,
        DxlAxl,

        // Used by Request
        Lock,
        Unlock,
        Disused,            // Mark for removal
        Erase,              // Disable
        Enter,              // Enable
        Start,              // Start thread
        Stop,               // Terminate thread
        Pause,              // Suspend thread
        Resume,             // Resume thread
        AddRender,
        SubRender,
        MulRender,
        Smoothing,          // Enable linear filtering
        Play,
        PushText,           // Disable typewriter animation
        NoLog,
        NoIcon,
        CompulsorySuspend,
        EntrustSuspend,
        Hideable,           // Possibly a no-op

        // Used by CreateTexture
        SCREEN,
        Video,

        // Used by CreateText
        Auto,
        Inherit,

        // Used by SetFont / SetShade
        Normal,
        Medium,
        Heavy,

        // Used by SetTone
        Monochrome,

        // Used by CreateScrollbar
        Vertical,
        Horizon,

        // Used by CreateSound
        BGM,
        SE,
        Voice,

        // Used by SetScore
        Local,
        Global,

        // Other
        Xxx,

        // Character names (Noah)
        ナイトハルト,
        梨深,
        星来,
        波多野,
        美愛,
        優愛,
        あやせ,
        拓巳,
        諏訪,
        将軍,
        男,
        葉月,
        太田,
        セナ,
        七海,
        女子Ａ,
        女子Ｂ,
        女子Ｃ,
        女子Ｄ,
        女子Ｅ,
        美菜子,
        若い女Ａ,
        若い男Ａ,
        若い男Ｂ,
        男子高校生Ａ,
        男子高校生Ｂ,
        女子高生Ａ,
        女子高生Ｂ,
        キャスターＡ,
        ケータイアナウンス
    }
}
