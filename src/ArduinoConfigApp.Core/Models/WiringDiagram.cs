namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Represents a complete wiring diagram with all components and connections
/// </summary>
public class WiringDiagram
{
    /// <summary>
    /// All components in the diagram
    /// </summary>
    public List<DiagramComponent> Components { get; set; } = [];

    /// <summary>
    /// All wire connections between components
    /// </summary>
    public List<WireConnection> Connections { get; set; } = [];

    /// <summary>
    /// Step-by-step wiring instructions
    /// </summary>
    public List<WiringStep> WiringSteps { get; set; } = [];

    /// <summary>
    /// Diagram width in pixels
    /// </summary>
    public int Width { get; set; } = 1200;

    /// <summary>
    /// Diagram height in pixels
    /// </summary>
    public int Height { get; set; } = 800;
}

/// <summary>
/// A component in the wiring diagram
/// </summary>
public class DiagramComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ComponentType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public List<DiagramPin> Pins { get; set; } = [];
}

/// <summary>
/// A pin on a diagram component
/// </summary>
public class DiagramPin
{
    public string Name { get; set; } = string.Empty;
    public int PinNumber { get; set; }
    public double RelativeX { get; set; }  // Relative to component position
    public double RelativeY { get; set; }
    public PinSide Side { get; set; }
}

/// <summary>
/// Which side of the component the pin is on
/// </summary>
public enum PinSide
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Types of components that can appear in the diagram
/// </summary>
public enum ComponentType
{
    ArduinoProMicro,
    ArduinoMega2560,
    MomentaryButton,
    LatchingButton,
    RotaryEncoder,
    ToggleSwitch,
    Max7219Display,
    PowerRail,
    GroundRail
}

/// <summary>
/// A wire connection between two pins
/// </summary>
public class WireConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Source component ID
    /// </summary>
    public Guid FromComponentId { get; set; }

    /// <summary>
    /// Source pin name
    /// </summary>
    public string FromPin { get; set; } = string.Empty;

    /// <summary>
    /// Destination component ID
    /// </summary>
    public Guid ToComponentId { get; set; }

    /// <summary>
    /// Destination pin name
    /// </summary>
    public string ToPin { get; set; } = string.Empty;

    /// <summary>
    /// Wire color for display
    /// </summary>
    public WireColor Color { get; set; } = WireColor.Blue;

    /// <summary>
    /// Optional label for the wire
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// Standard wire colors
/// </summary>
public enum WireColor
{
    Red,     // VCC/Power
    Black,   // Ground
    Blue,    // Signal
    Green,   // Signal alternate
    Yellow,  // Signal alternate
    Orange,  // SPI CLK
    Purple,  // SPI MOSI
    White,   // SPI CS
    Gray     // Other
}

/// <summary>
/// A single step in the wiring guide
/// </summary>
public class WiringStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WireConnection> ConnectionsInStep { get; set; } = [];
    public string? ImagePath { get; set; }
    public string? Warning { get; set; }
}
