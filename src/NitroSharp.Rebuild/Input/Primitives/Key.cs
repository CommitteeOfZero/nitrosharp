using static SDL.SDL_Scancode;

namespace NitroSharp.Input;

// Matches SDL3 scancodes
// Based on Veldrid.SDL2 the USB usage page standard:
// http://www.usb.org/developers/devclass_docs/Hut1_12v2.pdf
public enum Key
{
    Unknown = SDL_SCANCODE_UNKNOWN,

    // These values are from usage page 0x07 (USB keyboard page).

    A = SDL_SCANCODE_A,
    B = SDL_SCANCODE_B,
    C = SDL_SCANCODE_C,
    D = SDL_SCANCODE_D,
    E = SDL_SCANCODE_E,
    F = SDL_SCANCODE_F,
    G = SDL_SCANCODE_G,
    H = SDL_SCANCODE_H,
    I = SDL_SCANCODE_I,
    J = SDL_SCANCODE_J,
    K = SDL_SCANCODE_K,
    L = SDL_SCANCODE_L,
    M = SDL_SCANCODE_M,
    N = SDL_SCANCODE_N,
    O = SDL_SCANCODE_O,
    P = SDL_SCANCODE_P,
    Q = SDL_SCANCODE_Q,
    R = SDL_SCANCODE_R,
    S = SDL_SCANCODE_S,
    T = SDL_SCANCODE_T,
    U = SDL_SCANCODE_U,
    V = SDL_SCANCODE_V,
    W = SDL_SCANCODE_W,
    X = SDL_SCANCODE_X,
    Y = SDL_SCANCODE_Y,
    Z = SDL_SCANCODE_Z,

    Num1 = SDL_SCANCODE_1,
    Num2 = SDL_SCANCODE_2,
    Num3 = SDL_SCANCODE_3,
    Num4 = SDL_SCANCODE_4,
    Num5 = SDL_SCANCODE_5,
    Num6 = SDL_SCANCODE_6,
    Num7 = SDL_SCANCODE_7,
    Num8 = SDL_SCANCODE_8,
    Num9 = SDL_SCANCODE_9,
    Num0 = SDL_SCANCODE_0,

    Return = SDL_SCANCODE_RETURN,
    Escape = SDL_SCANCODE_ESCAPE,
    Backspace = SDL_SCANCODE_BACKSPACE,
    Tab = SDL_SCANCODE_TAB,
    Space = SDL_SCANCODE_SPACE,

    Minus = SDL_SCANCODE_MINUS,
    Equals = SDL_SCANCODE_EQUALS,
    LeftBracket = SDL_SCANCODE_LEFTBRACKET,
    RightBracket = SDL_SCANCODE_RIGHTBRACKET,

    /// <summary>
    /// Located at the lower left of the return key on ISO keyboards and at the right end of the QWERTY row on
    /// ANSI keyboards. Produces REVERSE SOLIDUS (backslash) and VERTICAL LINE in a US layout, REVERSE
    /// SOLIDUS and VERTICAL LINE in a UK Mac layout, NUMBER SIGN and TILDE in a UK Windows layout, DOLLAR SIGN
    /// and POUND SIGN in a Swiss German layout, NUMBER SIGN and APOSTROPHE in a German layout, GRAVE ACCENT and
    /// POUND SIGN in a French Mac layout, and ASTERISK and MICRO SIGN in a French Windows layout.
    /// </summary>
    Backslash = SDL_SCANCODE_BACKSLASH,

    /// <summary>
    /// ISO USB keyboards actually use this code
    /// instead of 49 for the same key, but all
    /// OSes I've seen treat the two codes
    /// identically. So, as an implementor, unless
    /// your keyboard generates both of those
    /// codes and your OS treats them differently,
    /// you should generate SDL_SCANCODE_BACKSLASH
    /// instead of this code. As a user, you
    /// should not rely on this code because SDL
    /// will never generate it with most (all?)
    /// keyboards.
    /// </summary>
    NonUsHash = SDL_SCANCODE_NONUSHASH,

    Semicolon = SDL_SCANCODE_SEMICOLON,
    Apostrophe = SDL_SCANCODE_APOSTROPHE,

    /// <summary>
    /// Located in the top left corner (on both ANSI
    /// and ISO keyboards). Produces GRAVE ACCENT and
    /// TILDE in a US Windows layout and in US and UK
    /// Mac layouts on ANSI keyboards, GRAVE ACCENT
    /// and NOT SIGN in a UK Windows layout, SECTION
    /// SIGN and PLUS-MINUS SIGN in US and UK Mac
    /// layouts on ISO keyboards, SECTION SIGN and
    /// DEGREE SIGN in a Swiss German layout (Mac:
    /// only on ISO keyboards), CIRCUMFLEX ACCENT and
    /// DEGREE SIGN in a German layout (Mac: only on
    /// ISO keyboards), SUPERSCRIPT TWO and TILDE in a
    /// French Windows layout, COMMERCIAL AT and
    /// NUMBER SIGN in a French Mac layout on ISO
    /// keyboards, and LESS-THAN SIGN and GREATER-THAN
    /// SIGN in a Swiss German, German, or French Mac
    /// layout on ANSI keyboards.
    /// </summary>
    Grave = SDL_SCANCODE_GRAVE,

    Comma = SDL_SCANCODE_COMMA,
    Period = SDL_SCANCODE_PERIOD,
    Slash = SDL_SCANCODE_SLASH,

    CapsLock = SDL_SCANCODE_CAPSLOCK,

    F1 = SDL_SCANCODE_F1,
    F2 = SDL_SCANCODE_F2,
    F3 = SDL_SCANCODE_F3,
    F4 = SDL_SCANCODE_F4,
    F5 = SDL_SCANCODE_F5,
    F6 = SDL_SCANCODE_F6,
    F7 = SDL_SCANCODE_F7,
    F8 = SDL_SCANCODE_F8,
    F9 = SDL_SCANCODE_F9,
    F10 = SDL_SCANCODE_F10,
    F11 = SDL_SCANCODE_F11,
    F12 = SDL_SCANCODE_F12,

    PrintScreen = SDL_SCANCODE_PRINTSCREEN,
    ScrollLock = SDL_SCANCODE_SCROLLLOCK,
    Pause = SDL_SCANCODE_PAUSE,

    /// <summary>
    /// insert on PC, help on some Mac keyboards (but does send code 73, not 117)
    /// </summary>
    Insert = SDL_SCANCODE_INSERT,

    Home = SDL_SCANCODE_HOME,
    PageUp = SDL_SCANCODE_PAGEUP,
    Delete = SDL_SCANCODE_DELETE,
    End = SDL_SCANCODE_END,
    PageDown = SDL_SCANCODE_PAGEDOWN,
    Right = SDL_SCANCODE_RIGHT,
    Left = SDL_SCANCODE_LEFT,
    Down = SDL_SCANCODE_DOWN,
    Up = SDL_SCANCODE_UP,

    /// <summary>
    /// num lock on PC, clear on Mac keyboards
    /// </summary>
    NumLockClear = SDL_SCANCODE_NUMLOCKCLEAR,

    KeypadDivide = SDL_SCANCODE_KP_DIVIDE,
    KeypadMultiply = SDL_SCANCODE_KP_MULTIPLY,
    KeypadMinus = SDL_SCANCODE_KP_MINUS,
    KeypadPlus = SDL_SCANCODE_KP_PLUS,
    KeypadEnter = SDL_SCANCODE_KP_ENTER,
    Keypad1 = SDL_SCANCODE_KP_1,
    Keypad2 = SDL_SCANCODE_KP_2,
    Keypad3 = SDL_SCANCODE_KP_3,
    Keypad4 = SDL_SCANCODE_KP_4,
    Keypad5 = SDL_SCANCODE_KP_5,
    Keypad6 = SDL_SCANCODE_KP_6,
    Keypad7 = SDL_SCANCODE_KP_7,
    Keypad8 = SDL_SCANCODE_KP_8,
    Keypad9 = SDL_SCANCODE_KP_9,
    Keypad0 = SDL_SCANCODE_KP_0,
    KeypadPeriod = SDL_SCANCODE_KP_PERIOD,

    /// <summary>
    /// This is the additional key that ISO
    /// keyboards have over ANSI ones,
    /// located between left shift and Y.
    /// Produces GRAVE ACCENT and TILDE in a
    /// US or UK Mac layout, REVERSE SOLIDUS
    /// (backslash) and VERTICAL LINE in a
    /// US or UK Windows layout, and
    /// LESS-THAN SIGN and GREATER-THAN SIGN
    /// in a Swiss German, German, or French layout.
    /// </summary>
    NonUsBackslash = SDL_SCANCODE_NONUSBACKSLASH,

    /// <summary>
    /// windows contextual menu, compose
    /// </summary>
    Application = SDL_SCANCODE_APPLICATION,

    /// <summary>
    /// The USB document says this is a status flag,
    /// not a physical key - but some Mac keyboards
    /// do have a power key.
    /// </summary>
    Power = SDL_SCANCODE_POWER,

    KeypadEquals = SDL_SCANCODE_KP_EQUALS,
    F13 = SDL_SCANCODE_F13,
    F14 = SDL_SCANCODE_F14,
    F15 = SDL_SCANCODE_F15,
    F16 = SDL_SCANCODE_F16,
    F17 = SDL_SCANCODE_F17,
    F18 = SDL_SCANCODE_F18,
    F19 = SDL_SCANCODE_F19,
    F20 = SDL_SCANCODE_F20,
    F21 = SDL_SCANCODE_F21,
    F22 = SDL_SCANCODE_F22,
    F23 = SDL_SCANCODE_F23,
    F24 = SDL_SCANCODE_F24,
    Execute = SDL_SCANCODE_EXECUTE,
    Help = SDL_SCANCODE_HELP,
    Menu = SDL_SCANCODE_MENU,
    Select = SDL_SCANCODE_SELECT,
    Stop = SDL_SCANCODE_STOP,

    Again = SDL_SCANCODE_AGAIN,

    Undo = SDL_SCANCODE_UNDO,
    Cut = SDL_SCANCODE_CUT,
    Copy = SDL_SCANCODE_COPY,
    Paste = SDL_SCANCODE_PASTE,
    Find = SDL_SCANCODE_FIND,
    Mute = SDL_SCANCODE_MUTE,
    VolumeUp = SDL_SCANCODE_VOLUMEUP,
    VolumeDown = SDL_SCANCODE_VOLUMEDOWN,
    KeypadComma = SDL_SCANCODE_KP_COMMA,
    KeypadEqualsAs400 = SDL_SCANCODE_KP_EQUALSAS400,

    International1 = SDL_SCANCODE_INTERNATIONAL1,

    International2 = SDL_SCANCODE_INTERNATIONAL2,

    /// <summary>
    /// Yen
    /// </summary>
    International3 = SDL_SCANCODE_INTERNATIONAL3,

    International4 = SDL_SCANCODE_INTERNATIONAL4,
    International5 = SDL_SCANCODE_INTERNATIONAL5,
    International6 = SDL_SCANCODE_INTERNATIONAL6,
    International7 = SDL_SCANCODE_INTERNATIONAL7,
    International8 = SDL_SCANCODE_INTERNATIONAL8,
    International9 = SDL_SCANCODE_INTERNATIONAL9,

    /// <summary>
    /// Hangul/English toggle
    /// </summary>
    Lang1 = SDL_SCANCODE_LANG1,

    /// <summary>
    /// Hanja conversion
    /// </summary>
    Lang2 = SDL_SCANCODE_LANG2,

    /// <summary>
    /// Katakana
    /// </summary>
    Lang3 = SDL_SCANCODE_LANG3,

    /// <summary>
    /// Hiragana
    /// </summary>
    Lang4 = SDL_SCANCODE_LANG4,

    /// <summary>
    /// Zenkaku/Hankaku
    /// </summary>
    Lang5 = SDL_SCANCODE_LANG5,
    Lang6 = SDL_SCANCODE_LANG6,
    Lang7 = SDL_SCANCODE_LANG7,
    Lang8 = SDL_SCANCODE_LANG8,
    Lang9 = SDL_SCANCODE_LANG9,

    /// <summary>
    /// Erase-Eaze
    /// </summary>
    AltErase = SDL_SCANCODE_ALTERASE,

    SysReq = SDL_SCANCODE_SYSREQ,
    Cancel = SDL_SCANCODE_CANCEL,
    Clear = SDL_SCANCODE_CLEAR,
    Prior = SDL_SCANCODE_PRIOR,
    Return2 = SDL_SCANCODE_RETURN2,
    Separator = SDL_SCANCODE_SEPARATOR,
    Out = SDL_SCANCODE_OUT,
    Oper = SDL_SCANCODE_OPER,
    ClearAgain = SDL_SCANCODE_CLEARAGAIN,
    CrSel = SDL_SCANCODE_CRSEL,
    ExSel = SDL_SCANCODE_EXSEL,

    Keypad00 = SDL_SCANCODE_KP_00,
    Keypad000 = SDL_SCANCODE_KP_000,
    ThousandsSeparator = SDL_SCANCODE_THOUSANDSSEPARATOR,
    DecimalSeparator = SDL_SCANCODE_DECIMALSEPARATOR,
    CurrencyUnit = SDL_SCANCODE_CURRENCYUNIT,
    CurrencySubunit = SDL_SCANCODE_CURRENCYSUBUNIT,
    KeypadLeftParen = SDL_SCANCODE_KP_LEFTPAREN,
    KeypadRightParen = SDL_SCANCODE_KP_RIGHTPAREN,
    KeypadLeftBrace = SDL_SCANCODE_KP_LEFTBRACE,
    KeypadRightBrace = SDL_SCANCODE_KP_RIGHTBRACE,
    KeypadTab = SDL_SCANCODE_KP_TAB,
    KeypadBackspace = SDL_SCANCODE_KP_BACKSPACE,
    KeypadA = SDL_SCANCODE_KP_A,
    KeypadB = SDL_SCANCODE_KP_B,
    KeypadC = SDL_SCANCODE_KP_C,
    KeypadD = SDL_SCANCODE_KP_D,
    KeypadE = SDL_SCANCODE_KP_E,
    KeypadF = SDL_SCANCODE_KP_F,
    KeypadXor = SDL_SCANCODE_KP_XOR,
    KeypadPower = SDL_SCANCODE_KP_POWER,
    KeypadPercent = SDL_SCANCODE_KP_PERCENT,
    KeypadLess = SDL_SCANCODE_KP_LESS,
    KeypadGreater = SDL_SCANCODE_KP_GREATER,
    KeypadAmpersand = SDL_SCANCODE_KP_AMPERSAND,
    KeypadDoubleAmpersand = SDL_SCANCODE_KP_DBLAMPERSAND,
    KeypadVerticalBar = SDL_SCANCODE_KP_VERTICALBAR,
    KeypadDoubleVerticalBar = SDL_SCANCODE_KP_DBLVERTICALBAR,
    KeypadColon = SDL_SCANCODE_KP_COLON,
    KeypadHash = SDL_SCANCODE_KP_HASH,
    KeypadSpace = SDL_SCANCODE_KP_SPACE,
    KeypadAt = SDL_SCANCODE_KP_AT,
    KeypadExclamation = SDL_SCANCODE_KP_EXCLAM,
    KeypadMemoryStore = SDL_SCANCODE_KP_MEMSTORE,
    KeypadMemoryRecall = SDL_SCANCODE_KP_MEMRECALL,
    KeypadMemoryClear = SDL_SCANCODE_KP_MEMCLEAR,
    KeypadMemoryAdd = SDL_SCANCODE_KP_MEMADD,
    KeypadMemorySubtract = SDL_SCANCODE_KP_MEMSUBTRACT,
    KeypadMemoryMultiply = SDL_SCANCODE_KP_MEMMULTIPLY,
    KeypadMemoryDivide = SDL_SCANCODE_KP_MEMDIVIDE,
    KeypadPlusMinus = SDL_SCANCODE_KP_PLUSMINUS,
    KeypadClear = SDL_SCANCODE_KP_CLEAR,
    KeypadClearEntry = SDL_SCANCODE_KP_CLEARENTRY,
    KeypadBinary = SDL_SCANCODE_KP_BINARY,
    KeypadOctal = SDL_SCANCODE_KP_OCTAL,
    KeypadDecimal = SDL_SCANCODE_KP_DECIMAL,
    KeypadHexadecimal = SDL_SCANCODE_KP_HEXADECIMAL,

    LeftControl = SDL_SCANCODE_LCTRL,
    LeftShift = SDL_SCANCODE_LSHIFT,

    /// <summary>
    ///  alt, option
    /// </summary>
    LeftAlt = SDL_SCANCODE_LALT,

    /// <summary>
    /// windows, command (apple), meta
    /// </summary>
    LeftGui = SDL_SCANCODE_LGUI,

    RightControl = SDL_SCANCODE_RCTRL,
    RightShift = SDL_SCANCODE_RSHIFT,

    /// <summary>
    /// alt gr, option
    /// </summary>
    RightAlt = SDL_SCANCODE_RALT,

    /// <summary>
    /// windows, command (apple), meta
    /// </summary>
    RightGui = SDL_SCANCODE_RGUI,

    Mode = SDL_SCANCODE_MODE,


    // Usage page 0x0C
    // These values are mapped from usage page 0x0C (USB consumer page).

    MediaNext = SDL_SCANCODE_MEDIA_NEXT_TRACK,
    MediaPrevious = SDL_SCANCODE_MEDIA_PREVIOUS_TRACK,
    MediaStop = SDL_SCANCODE_MEDIA_STOP,
    MediaPlay = SDL_SCANCODE_MEDIA_PLAY,
    MediaSelect = SDL_SCANCODE_MEDIA_SELECT,

    AppControlSearch = SDL_SCANCODE_AC_SEARCH,
    AppControlHome = SDL_SCANCODE_AC_HOME,
    AppControlBack = SDL_SCANCODE_AC_BACK,
    AppControlForward = SDL_SCANCODE_AC_FORWARD,
    AppControlStop = SDL_SCANCODE_AC_STOP,
    AppControlRefresh = SDL_SCANCODE_AC_REFRESH,
    AppControlBookmarks = SDL_SCANCODE_AC_BOOKMARKS,


    Eject = SDL_SCANCODE_MEDIA_EJECT,
    Sleep = SDL_SCANCODE_SLEEP,

    /// <summary>
    /// Not a key, just marks the number of scancodes for enum.
    /// </summary>
    NumKeys = SDL_SCANCODE_COUNT,
}
