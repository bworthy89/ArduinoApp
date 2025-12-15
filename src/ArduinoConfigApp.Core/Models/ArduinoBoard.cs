using ArduinoConfigApp.Core.Enums;

namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Represents an Arduino board with its specifications and connection state
/// </summary>
public class ArduinoBoard
{
    /// <summary>
    /// Unique identifier for this board instance
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly name for this board
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of Arduino board
    /// </summary>
    public BoardType BoardType { get; set; }

    /// <summary>
    /// COM port the board is connected to (e.g., "COM3")
    /// </summary>
    public string? PortName { get; set; }

    /// <summary>
    /// Current connection state
    /// </summary>
    public ConnectionState ConnectionState { get; set; } = ConnectionState.Disconnected;

    /// <summary>
    /// Firmware version reported by the board (if connected)
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Last time the board was successfully connected
    /// </summary>
    public DateTime? LastConnected { get; set; }

    /// <summary>
    /// Gets the available digital pins for this board type
    /// </summary>
    public IReadOnlyList<PinInfo> AvailablePins => BoardType switch
    {
        BoardType.ProMicro => ProMicroPins,
        BoardType.Mega2560 => Mega2560Pins,
        _ => Array.Empty<PinInfo>()
    };

    /// <summary>
    /// Gets the SPI pins for this board type (for MAX7219)
    /// </summary>
    public SpiPins SpiPins => BoardType switch
    {
        BoardType.ProMicro => new SpiPins(15, 16, 14),  // SCK=15, MOSI=16, SS=14
        BoardType.Mega2560 => new SpiPins(52, 51, 53), // SCK=52, MOSI=51, SS=53
        _ => new SpiPins(0, 0, 0)
    };

    /// <summary>
    /// Whether this board supports native USB HID (keyboard emulation)
    /// </summary>
    public bool SupportsNativeHid => BoardType == BoardType.ProMicro;

    // Pin definitions for Pro Micro
    private static readonly PinInfo[] ProMicroPins =
    [
        new(2, "D2", PinCapability.Digital | PinCapability.Interrupt),
        new(3, "D3", PinCapability.Digital | PinCapability.Interrupt | PinCapability.Pwm),
        new(4, "D4", PinCapability.Digital),
        new(5, "D5", PinCapability.Digital | PinCapability.Pwm),
        new(6, "D6", PinCapability.Digital | PinCapability.Pwm),
        new(7, "D7", PinCapability.Digital | PinCapability.Interrupt),
        new(8, "D8", PinCapability.Digital),
        new(9, "D9", PinCapability.Digital | PinCapability.Pwm),
        new(10, "D10", PinCapability.Digital | PinCapability.Pwm),
        new(14, "D14/MISO", PinCapability.Digital | PinCapability.Spi),
        new(15, "D15/SCK", PinCapability.Digital | PinCapability.Spi),
        new(16, "D16/MOSI", PinCapability.Digital | PinCapability.Spi),
        new(18, "A0", PinCapability.Digital | PinCapability.Analog),
        new(19, "A1", PinCapability.Digital | PinCapability.Analog),
        new(20, "A2", PinCapability.Digital | PinCapability.Analog),
        new(21, "A3", PinCapability.Digital | PinCapability.Analog),
    ];

    // Pin definitions for Mega 2560
    private static readonly PinInfo[] Mega2560Pins =
    [
        new(2, "D2", PinCapability.Digital | PinCapability.Interrupt | PinCapability.Pwm),
        new(3, "D3", PinCapability.Digital | PinCapability.Interrupt | PinCapability.Pwm),
        new(4, "D4", PinCapability.Digital | PinCapability.Pwm),
        new(5, "D5", PinCapability.Digital | PinCapability.Pwm),
        new(6, "D6", PinCapability.Digital | PinCapability.Pwm),
        new(7, "D7", PinCapability.Digital | PinCapability.Pwm),
        new(8, "D8", PinCapability.Digital | PinCapability.Pwm),
        new(9, "D9", PinCapability.Digital | PinCapability.Pwm),
        new(10, "D10", PinCapability.Digital | PinCapability.Pwm),
        new(11, "D11", PinCapability.Digital | PinCapability.Pwm),
        new(12, "D12", PinCapability.Digital | PinCapability.Pwm),
        new(13, "D13", PinCapability.Digital | PinCapability.Pwm),
        new(18, "D18/TX1", PinCapability.Digital | PinCapability.Interrupt),
        new(19, "D19/RX1", PinCapability.Digital | PinCapability.Interrupt),
        new(20, "D20/SDA", PinCapability.Digital | PinCapability.Interrupt | PinCapability.I2c),
        new(21, "D21/SCL", PinCapability.Digital | PinCapability.Interrupt | PinCapability.I2c),
        new(22, "D22", PinCapability.Digital),
        new(23, "D23", PinCapability.Digital),
        new(24, "D24", PinCapability.Digital),
        new(25, "D25", PinCapability.Digital),
        new(26, "D26", PinCapability.Digital),
        new(27, "D27", PinCapability.Digital),
        new(28, "D28", PinCapability.Digital),
        new(29, "D29", PinCapability.Digital),
        new(30, "D30", PinCapability.Digital),
        new(31, "D31", PinCapability.Digital),
        new(32, "D32", PinCapability.Digital),
        new(33, "D33", PinCapability.Digital),
        new(34, "D34", PinCapability.Digital),
        new(35, "D35", PinCapability.Digital),
        new(36, "D36", PinCapability.Digital),
        new(37, "D37", PinCapability.Digital),
        new(38, "D38", PinCapability.Digital),
        new(39, "D39", PinCapability.Digital),
        new(40, "D40", PinCapability.Digital),
        new(41, "D41", PinCapability.Digital),
        new(42, "D42", PinCapability.Digital),
        new(43, "D43", PinCapability.Digital),
        new(44, "D44", PinCapability.Digital | PinCapability.Pwm),
        new(45, "D45", PinCapability.Digital | PinCapability.Pwm),
        new(46, "D46", PinCapability.Digital | PinCapability.Pwm),
        new(50, "D50/MISO", PinCapability.Digital | PinCapability.Spi),
        new(51, "D51/MOSI", PinCapability.Digital | PinCapability.Spi),
        new(52, "D52/SCK", PinCapability.Digital | PinCapability.Spi),
        new(53, "D53/SS", PinCapability.Digital | PinCapability.Spi),
        new(54, "A0", PinCapability.Digital | PinCapability.Analog),
        new(55, "A1", PinCapability.Digital | PinCapability.Analog),
        new(56, "A2", PinCapability.Digital | PinCapability.Analog),
        new(57, "A3", PinCapability.Digital | PinCapability.Analog),
        new(58, "A4", PinCapability.Digital | PinCapability.Analog),
        new(59, "A5", PinCapability.Digital | PinCapability.Analog),
        new(60, "A6", PinCapability.Digital | PinCapability.Analog),
        new(61, "A7", PinCapability.Digital | PinCapability.Analog),
    ];
}

/// <summary>
/// Information about a single Arduino pin
/// </summary>
public record PinInfo(int PinNumber, string Label, PinCapability Capabilities);

/// <summary>
/// SPI pin assignments for a board
/// </summary>
public record SpiPins(int Sck, int Mosi, int Ss);

/// <summary>
/// Pin capabilities flags
/// </summary>
[Flags]
public enum PinCapability
{
    None = 0,
    Digital = 1,
    Analog = 2,
    Pwm = 4,
    Interrupt = 8,
    Spi = 16,
    I2c = 32
}
