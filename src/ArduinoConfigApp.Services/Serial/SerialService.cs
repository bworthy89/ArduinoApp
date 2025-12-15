using System.IO.Ports;
using System.Management;
using System.Text;
using System.Text.Json;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;

namespace ArduinoConfigApp.Services.Serial;

/// <summary>
/// Implementation of serial communication service for Arduino boards
/// Uses System.IO.Ports for serial communication
/// </summary>
public class SerialService : ISerialService
{
    private SerialPort? _serialPort;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _readCancellation;
    private Task? _readTask;
    private readonly StringBuilder _receiveBuffer = new();

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<ArduinoDataReceivedEventArgs>? DataReceived;
    public event EventHandler<SerialErrorEventArgs>? ErrorOccurred;

    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
    public string? CurrentPort => _serialPort?.PortName;

    /// <summary>
    /// Gets all available COM ports with Arduino detection
    /// </summary>
    public Task<IReadOnlyList<PortInfo>> GetAvailablePortsAsync()
    {
        return Task.Run(() =>
        {
            var ports = new List<PortInfo>();
            var portNames = SerialPort.GetPortNames();

            foreach (var portName in portNames)
            {
                var description = GetPortDescription(portName);
                var isArduino = IsArduinoPort(description);
                ports.Add(new PortInfo(portName, description, isArduino));
            }

            return (IReadOnlyList<PortInfo>)ports.OrderBy(p => p.PortName).ToList();
        });
    }

    /// <summary>
    /// Gets port description from Windows Management
    /// </summary>
    private static string GetPortDescription(string portName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%" + portName + "%'");

            foreach (var obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString();
                if (!string.IsNullOrEmpty(caption))
                    return caption;
            }
        }
        catch
        {
            // WMI not available or error - fall back to port name
        }

        return portName;
    }

    /// <summary>
    /// Detects if a port is likely an Arduino
    /// </summary>
    private static bool IsArduinoPort(string description)
    {
        var lowerDesc = description.ToLowerInvariant();
        return lowerDesc.Contains("arduino") ||
               lowerDesc.Contains("ch340") ||       // Common USB-serial chip
               lowerDesc.Contains("ch341") ||
               lowerDesc.Contains("ftdi") ||
               lowerDesc.Contains("usb serial") ||
               lowerDesc.Contains("atmega32u4") ||  // Pro Micro
               lowerDesc.Contains("atmega2560");    // Mega 2560
    }

    /// <summary>
    /// Connects to the specified COM port
    /// </summary>
    public async Task<bool> ConnectAsync(string portName, int baudRate = 115200)
    {
        if (ConnectionState == ConnectionState.Connected)
        {
            await DisconnectAsync();
        }

        SetConnectionState(ConnectionState.Connecting, portName);

        try
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                DtrEnable = true,  // Required for Arduino auto-reset
                RtsEnable = true
            };

            _serialPort.Open();

            // Wait for Arduino to reset (it resets on connection)
            await Task.Delay(2000);

            // Clear any garbage data
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            // Start background reading task
            _readCancellation = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_readCancellation.Token));

            // Verify connection with ping
            var pingSuccess = await PingAsync();
            if (!pingSuccess)
            {
                await DisconnectAsync();
                RaiseError("Failed to communicate with Arduino. Ensure firmware is uploaded.");
                return false;
            }

            SetConnectionState(ConnectionState.Connected, portName);
            return true;
        }
        catch (Exception ex)
        {
            SetConnectionState(ConnectionState.Error, portName);
            RaiseError($"Failed to connect: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the current port
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_readCancellation != null)
        {
            _readCancellation.Cancel();
            if (_readTask != null)
            {
                try { await _readTask; } catch { /* Ignore cancellation */ }
            }
            _readCancellation.Dispose();
            _readCancellation = null;
        }

        if (_serialPort != null)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            _serialPort.Dispose();
            _serialPort = null;
        }

        SetConnectionState(ConnectionState.Disconnected, null);
    }

    /// <summary>
    /// Background task that continuously reads from serial port
    /// </summary>
    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested && _serialPort?.IsOpen == true)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    var bytesRead = await _serialPort.BaseStream.ReadAsync(
                        buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessReceivedData(data, buffer[..bytesRead]);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    RaiseError($"Read error: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Processes received data and raises events
    /// </summary>
    private void ProcessReceivedData(string data, byte[] rawData)
    {
        _receiveBuffer.Append(data);

        // Process complete lines (JSON messages end with newline)
        var bufferContent = _receiveBuffer.ToString();
        var lines = bufferContent.Split('\n');

        for (int i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                DataReceived?.Invoke(this, new ArduinoDataReceivedEventArgs
                {
                    Data = line,
                    RawData = Encoding.UTF8.GetBytes(line)
                });
            }
        }

        // Keep the incomplete last line in buffer
        _receiveBuffer.Clear();
        _receiveBuffer.Append(lines[^1]);
    }

    /// <summary>
    /// Sends a command and waits for response
    /// </summary>
    public async Task<SerialResponse> SendCommandAsync(SerialCommand command, CancellationToken cancellationToken = default)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
        {
            return new SerialResponse { Success = false, ErrorMessage = "Not connected" };
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            // Serialize command to JSON
            var json = JsonSerializer.Serialize(new
            {
                cmd = command.CommandType,
                @params = command.Parameters
            });

            // Create response completion source
            var responseReceived = new TaskCompletionSource<string>();

            void OnDataReceived(object? sender, ArduinoDataReceivedEventArgs e)
            {
                if (e.Data.StartsWith("{") && e.Data.Contains("\"response\""))
                {
                    responseReceived.TrySetResult(e.Data);
                }
            }

            DataReceived += OnDataReceived;

            try
            {
                // Send command
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                await _serialPort.BaseStream.WriteAsync(bytes, cancellationToken);

                // Wait for response with timeout
                using var timeoutCts = new CancellationTokenSource(command.TimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                var responseJson = await responseReceived.Task.WaitAsync(linkedCts.Token);

                // Parse response
                var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
                return new SerialResponse
                {
                    Success = response.TryGetProperty("success", out var success) && success.GetBoolean(),
                    Data = response.TryGetProperty("data", out var data) ? data.ToString() : null,
                    ErrorMessage = response.TryGetProperty("error", out var error) ? error.GetString() : null
                };
            }
            finally
            {
                DataReceived -= OnDataReceived;
            }
        }
        catch (OperationCanceledException)
        {
            return new SerialResponse { Success = false, ErrorMessage = "Command timeout" };
        }
        catch (Exception ex)
        {
            return new SerialResponse { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Sends raw data to the Arduino
    /// </summary>
    public async Task SendRawAsync(byte[] data)
    {
        if (_serialPort == null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Not connected");

        await _sendLock.WaitAsync();
        try
        {
            await _serialPort.BaseStream.WriteAsync(data);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Pings the Arduino to verify connection
    /// </summary>
    public async Task<bool> PingAsync()
    {
        var response = await SendCommandAsync(new SerialCommand
        {
            CommandType = "PING",
            TimeoutMs = 2000
        });

        return response.Success;
    }

    /// <summary>
    /// Gets firmware version from Arduino
    /// </summary>
    public async Task<string?> GetFirmwareVersionAsync()
    {
        var response = await SendCommandAsync(new SerialCommand
        {
            CommandType = "VERSION",
            TimeoutMs = 1000
        });

        return response.Success ? response.Data : null;
    }

    private void SetConnectionState(ConnectionState newState, string? portName)
    {
        var oldState = ConnectionState;
        ConnectionState = newState;

        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            PortName = portName
        });
    }

    private void RaiseError(string message, Exception? ex = null)
    {
        ErrorOccurred?.Invoke(this, new SerialErrorEventArgs
        {
            Message = message,
            Exception = ex
        });
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _sendLock.Dispose();
    }
}
