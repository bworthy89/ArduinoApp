namespace ArduinoConfigApp.Core.Enums;

/// <summary>
/// Supported Arduino board types
/// </summary>
public enum BoardType
{
    /// <summary>
    /// Arduino Pro Micro (ATmega32U4) - supports native USB HID
    /// </summary>
    ProMicro,

    /// <summary>
    /// Arduino Mega 2560 (ATmega2560) - more pins, no native HID
    /// </summary>
    Mega2560
}
