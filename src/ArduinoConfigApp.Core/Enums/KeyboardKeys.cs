namespace ArduinoConfigApp.Core.Enums;

/// <summary>
/// Keyboard keys supported for output mapping
/// Maps to Arduino Keyboard library key codes
/// </summary>
public enum KeyboardKey
{
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Special keys
    Enter,
    Escape,
    Backspace,
    Tab,
    Space,
    Delete,
    Insert,
    Home,
    End,
    PageUp,
    PageDown,

    // Arrow keys
    UpArrow,
    DownArrow,
    LeftArrow,
    RightArrow,

    // Modifiers (can be combined)
    LeftCtrl,
    LeftShift,
    LeftAlt,
    LeftGui,  // Windows key
    RightCtrl,
    RightShift,
    RightAlt,
    RightGui,

    // Numpad
    NumpadMultiply,
    NumpadPlus,
    NumpadMinus,
    NumpadDivide,
    NumpadEnter,

    // Symbols
    Minus,
    Equals,
    LeftBracket,
    RightBracket,
    Backslash,
    Semicolon,
    Quote,
    Grave,
    Comma,
    Period,
    Slash,

    // Media keys (requires consumer HID)
    MediaPlayPause,
    MediaStop,
    MediaNext,
    MediaPrevious,
    MediaVolumeUp,
    MediaVolumeDown,
    MediaMute
}

/// <summary>
/// Modifier keys for keyboard combinations
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Gui = 8  // Windows key
}
