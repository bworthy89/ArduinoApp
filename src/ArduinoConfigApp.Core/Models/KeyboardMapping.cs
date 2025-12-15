using ArduinoConfigApp.Core.Enums;

namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Maps an input action to a keyboard output
/// </summary>
public class KeyboardMapping
{
    /// <summary>
    /// Unique identifier for this mapping
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the input this mapping is for
    /// </summary>
    public Guid InputId { get; set; }

    /// <summary>
    /// The trigger action that activates this mapping
    /// </summary>
    public InputAction TriggerAction { get; set; }

    /// <summary>
    /// Primary key to send
    /// </summary>
    public KeyboardKey Key { get; set; }

    /// <summary>
    /// Modifier keys to combine with the primary key
    /// </summary>
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;

    /// <summary>
    /// Whether to hold the key while input is active (for toggle switches)
    /// </summary>
    public bool HoldWhileActive { get; set; } = false;

    /// <summary>
    /// Whether this mapping is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// User-friendly description of this mapping
    /// </summary>
    public string Description => GenerateDescription();

    private string GenerateDescription()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Gui)) parts.Add("Win");

        parts.Add(Key.ToString());

        return string.Join(" + ", parts);
    }
}

/// <summary>
/// Input actions that can trigger keyboard outputs
/// </summary>
public enum InputAction
{
    /// <summary>
    /// Button pressed (for momentary buttons)
    /// </summary>
    Press,

    /// <summary>
    /// Button released
    /// </summary>
    Release,

    /// <summary>
    /// Button held for a duration
    /// </summary>
    Hold,

    /// <summary>
    /// Encoder rotated clockwise
    /// </summary>
    RotateClockwise,

    /// <summary>
    /// Encoder rotated counter-clockwise
    /// </summary>
    RotateCounterClockwise,

    /// <summary>
    /// Encoder button pressed
    /// </summary>
    EncoderPress,

    /// <summary>
    /// Toggle switch turned on
    /// </summary>
    ToggleOn,

    /// <summary>
    /// Toggle switch turned off
    /// </summary>
    ToggleOff
}

/// <summary>
/// A macro sequence of keyboard actions
/// </summary>
public class KeyboardMacro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid InputId { get; set; }
    public InputAction TriggerAction { get; set; }
    public List<MacroStep> Steps { get; set; } = [];
}

/// <summary>
/// A single step in a macro sequence
/// </summary>
public class MacroStep
{
    public KeyboardKey Key { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public MacroAction Action { get; set; }
    public int DelayMs { get; set; } = 0;
}

/// <summary>
/// Action type for macro steps
/// </summary>
public enum MacroAction
{
    Press,
    Release,
    Tap,  // Press and release
    Delay
}
