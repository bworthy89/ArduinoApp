using ArduinoConfigApp.Core.Enums;

namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Base class for all input configurations
/// </summary>
public abstract class InputConfiguration
{
    /// <summary>
    /// Unique identifier for this input
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly name for this input
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of input component
    /// </summary>
    public abstract InputType InputType { get; }

    /// <summary>
    /// Whether this input is enabled in the configuration
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets all pin numbers used by this input (for conflict detection)
    /// </summary>
    public abstract IReadOnlyList<int> UsedPins { get; }
}

/// <summary>
/// Configuration for a button input (latching or momentary)
/// </summary>
public class ButtonConfiguration : InputConfiguration
{
    /// <summary>
    /// Digital pin the button is connected to
    /// </summary>
    public int Pin { get; set; }

    /// <summary>
    /// Whether this is a latching button (true) or momentary (false)
    /// </summary>
    public bool IsLatching { get; set; }

    /// <summary>
    /// Use internal pull-up resistor
    /// </summary>
    public bool UseInternalPullup { get; set; } = true;

    /// <summary>
    /// Debounce time in milliseconds
    /// </summary>
    public int DebounceMs { get; set; } = 50;

    public override InputType InputType => IsLatching ? InputType.LatchingButton : InputType.MomentaryButton;

    public override IReadOnlyList<int> UsedPins => [Pin];
}

/// <summary>
/// Configuration for an EC11 rotary encoder
/// </summary>
public class EncoderConfiguration : InputConfiguration
{
    /// <summary>
    /// Pin A (CLK) of the encoder
    /// </summary>
    public int PinA { get; set; }

    /// <summary>
    /// Pin B (DT) of the encoder
    /// </summary>
    public int PinB { get; set; }

    /// <summary>
    /// Pin for the encoder's push button (SW), -1 if not used
    /// </summary>
    public int ButtonPin { get; set; } = -1;

    /// <summary>
    /// Use internal pull-up resistors
    /// </summary>
    public bool UseInternalPullups { get; set; } = true;

    /// <summary>
    /// Steps per detent (typically 4 for EC11)
    /// </summary>
    public int StepsPerDetent { get; set; } = 4;

    /// <summary>
    /// ID of the display this encoder controls (if any)
    /// </summary>
    public Guid? LinkedDisplayId { get; set; }

    /// <summary>
    /// Increment value when encoder is rotated
    /// </summary>
    public EncoderIncrement Increment { get; set; } = EncoderIncrement.One;

    /// <summary>
    /// Minimum value for the encoder
    /// </summary>
    public int MinValue { get; set; } = 0;

    /// <summary>
    /// Maximum value for the encoder
    /// </summary>
    public int MaxValue { get; set; } = 99999999;

    /// <summary>
    /// Current value of the encoder
    /// </summary>
    public int CurrentValue { get; set; } = 0;

    public override InputType InputType => InputType.RotaryEncoder;

    public override IReadOnlyList<int> UsedPins =>
        ButtonPin >= 0 ? [PinA, PinB, ButtonPin] : [PinA, PinB];
}

/// <summary>
/// Configuration for a toggle switch
/// </summary>
public class ToggleSwitchConfiguration : InputConfiguration
{
    /// <summary>
    /// Digital pin the switch is connected to
    /// </summary>
    public int Pin { get; set; }

    /// <summary>
    /// Use internal pull-up resistor
    /// </summary>
    public bool UseInternalPullup { get; set; } = true;

    /// <summary>
    /// Debounce time in milliseconds
    /// </summary>
    public int DebounceMs { get; set; } = 50;

    /// <summary>
    /// Current state of the toggle switch
    /// </summary>
    public bool IsOn { get; set; }

    public override InputType InputType => InputType.ToggleSwitch;

    public override IReadOnlyList<int> UsedPins => [Pin];
}
