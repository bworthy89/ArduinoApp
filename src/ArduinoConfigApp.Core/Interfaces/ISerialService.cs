using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Core.Interfaces;

/// <summary>
/// Service for serial communication with Arduino boards
/// </summary>
public interface ISerialService : IAsyncDisposable
{
    /// <summary>
    /// Event raised when connection state changes
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when data is received from the Arduino
    /// </summary>
    event EventHandler<SerialDataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<SerialErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Current connection state
    /// </summary>
    ConnectionState ConnectionState { get; }

    /// <summary>
    /// Currently connected port name
    /// </summary>
    string? CurrentPort { get; }

    /// <summary>
    /// Gets available COM ports
    /// </summary>
    Task<IReadOnlyList<PortInfo>> GetAvailablePortsAsync();

    /// <summary>
    /// Connects to the specified COM port
    /// </summary>
    Task<bool> ConnectAsync(string portName, int baudRate = 115200);

    /// <summary>
    /// Disconnects from the current port
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a command to the Arduino and waits for response
    /// </summary>
    Task<SerialResponse> SendCommandAsync(SerialCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends raw data to the Arduino
    /// </summary>
    Task SendRawAsync(byte[] data);

    /// <summary>
    /// Pings the Arduino to check connection
    /// </summary>
    Task<bool> PingAsync();

    /// <summary>
    /// Gets the firmware version from the connected Arduino
    /// </summary>
    Task<string?> GetFirmwareVersionAsync();
}

/// <summary>
/// Information about a COM port
/// </summary>
public record PortInfo(string PortName, string Description, bool IsArduino);

/// <summary>
/// Event args for connection state changes
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionState OldState { get; init; }
    public ConnectionState NewState { get; init; }
    public string? PortName { get; init; }
}

/// <summary>
/// Event args for received serial data
/// </summary>
public class SerialDataReceivedEventArgs : EventArgs
{
    public string Data { get; init; } = string.Empty;
    public byte[]? RawData { get; init; }
}

/// <summary>
/// Event args for serial errors
/// </summary>
public class SerialErrorEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}

/// <summary>
/// Represents a command to send to the Arduino
/// </summary>
public class SerialCommand
{
    public string CommandType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = [];
    public int TimeoutMs { get; set; } = 1000;
}

/// <summary>
/// Response from the Arduino
/// </summary>
public class SerialResponse
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> ParsedData { get; set; } = [];
}
