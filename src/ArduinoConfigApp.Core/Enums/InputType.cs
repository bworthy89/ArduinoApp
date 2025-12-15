namespace ArduinoConfigApp.Core.Enums;

/// <summary>
/// Types of input components supported by the application
/// </summary>
public enum InputType
{
    /// <summary>
    /// KD2-22 Latching push button - stays pressed until pressed again
    /// </summary>
    LatchingButton,

    /// <summary>
    /// Standard momentary push button - active only while pressed
    /// </summary>
    MomentaryButton,

    /// <summary>
    /// EC11 Rotary encoder with push button - provides rotation and click
    /// </summary>
    RotaryEncoder,

    /// <summary>
    /// Toggle switch - ON/OFF positions
    /// </summary>
    ToggleSwitch
}

/// <summary>
/// Rotary encoder increment values for display mapping
/// </summary>
public enum EncoderIncrement
{
    One = 1,
    Ten = 10,
    Hundred = 100,
    Thousand = 1000
}

/// <summary>
/// Connection state of the Arduino board
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
