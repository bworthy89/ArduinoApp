namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Configuration for a MAX7219-based 7-segment display module
/// </summary>
public class DisplayConfiguration
{
    /// <summary>
    /// Unique identifier for this display
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly name for this display
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Chip Select (CS/LOAD) pin for this display's MAX7219
    /// </summary>
    public int CsPin { get; set; }

    /// <summary>
    /// Number of digits on the display (typically 8 for 8-digit module)
    /// </summary>
    public int DigitCount { get; set; } = 8;

    /// <summary>
    /// Display brightness (0-15)
    /// </summary>
    public int Brightness { get; set; } = 8;

    /// <summary>
    /// Number of cascaded MAX7219 chips (for larger displays)
    /// </summary>
    public int ChainedDevices { get; set; } = 1;

    /// <summary>
    /// Whether to show leading zeros
    /// </summary>
    public bool ShowLeadingZeros { get; set; } = false;

    /// <summary>
    /// Current value being displayed
    /// </summary>
    public long CurrentValue { get; set; } = 0;

    /// <summary>
    /// IDs of encoders that control this display
    /// Used for conflict detection and management
    /// </summary>
    public List<Guid> LinkedEncoderIds { get; set; } = [];

    /// <summary>
    /// Whether this display is currently active/enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Decimal point position (0 = rightmost, -1 = none)
    /// </summary>
    public int DecimalPosition { get; set; } = -1;
}

/// <summary>
/// Represents the runtime state of a display for testing/preview
/// </summary>
public class DisplayState
{
    public Guid DisplayId { get; set; }
    public long Value { get; set; }
    public int Brightness { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public bool[] SegmentStates { get; set; } = new bool[8];
}
