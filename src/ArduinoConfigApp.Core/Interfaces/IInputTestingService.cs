using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Core.Interfaces;

/// <summary>
/// Service for real-time input testing and monitoring
/// </summary>
public interface IInputTestingService : IDisposable
{
    /// <summary>
    /// Event raised when input states are updated
    /// </summary>
    event EventHandler<InputStatesUpdatedEventArgs>? InputStatesUpdated;

    /// <summary>
    /// Event raised when a specific input is triggered
    /// </summary>
    event EventHandler<InputTriggeredEventArgs>? InputTriggered;

    /// <summary>
    /// Whether testing is currently active
    /// </summary>
    bool IsTestingActive { get; }

    /// <summary>
    /// Polling interval in milliseconds
    /// </summary>
    int PollingIntervalMs { get; set; }

    /// <summary>
    /// Starts polling input states from the Arduino
    /// </summary>
    Task StartTestingAsync();

    /// <summary>
    /// Stops polling input states
    /// </summary>
    Task StopTestingAsync();

    /// <summary>
    /// Gets the current state of all inputs
    /// </summary>
    IReadOnlyDictionary<Guid, InputState> GetCurrentStates();

    /// <summary>
    /// Gets the current state of a specific input
    /// </summary>
    InputState? GetInputState(Guid inputId);

    /// <summary>
    /// Simulates an input trigger (for testing without hardware)
    /// </summary>
    void SimulateTrigger(Guid inputId, InputAction action);

    /// <summary>
    /// Tests a specific display by showing a test pattern
    /// </summary>
    Task TestDisplayAsync(Guid displayId, DisplayTestPattern pattern);

    /// <summary>
    /// Sets a specific value on a display
    /// </summary>
    Task SetDisplayValueAsync(Guid displayId, long value);
}

/// <summary>
/// Current state of an input
/// </summary>
public class InputState
{
    public Guid InputId { get; set; }
    public string InputName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? EncoderValue { get; set; }
    public DateTime LastChanged { get; set; }
    public int TriggerCount { get; set; }
}

/// <summary>
/// Event args for input state updates
/// </summary>
public class InputStatesUpdatedEventArgs : EventArgs
{
    public IReadOnlyDictionary<Guid, InputState> States { get; init; } = new Dictionary<Guid, InputState>();
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Event args for input triggers
/// </summary>
public class InputTriggeredEventArgs : EventArgs
{
    public Guid InputId { get; init; }
    public string InputName { get; init; } = string.Empty;
    public InputAction Action { get; init; }
    public int? Value { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Test patterns for displays
/// </summary>
public enum DisplayTestPattern
{
    AllOn,
    AllOff,
    CountUp,
    CountDown,
    Sweep,
    Random
}
