using System.Text.Json;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;
using ArduinoConfigApp.Services.Serial;

namespace ArduinoConfigApp.Services.InputTesting;

/// <summary>
/// Service for real-time input testing with Arduino hardware
/// Polls input states and provides visual feedback data
/// </summary>
public class InputTestingService : IInputTestingService
{
    private readonly ISerialService _serialService;
    private readonly IConfigurationService _configService;

    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private readonly Dictionary<Guid, InputState> _currentStates = new();
    private readonly object _stateLock = new();

    public event EventHandler<InputStatesUpdatedEventArgs>? InputStatesUpdated;
    public event EventHandler<InputTriggeredEventArgs>? InputTriggered;

    public bool IsTestingActive { get; private set; }
    public int PollingIntervalMs { get; set; } = 50; // 20 Hz default

    public InputTestingService(ISerialService serialService, IConfigurationService configService)
    {
        _serialService = serialService;
        _configService = configService;

        // Subscribe to serial data for event-driven updates
        _serialService.DataReceived += OnSerialDataReceived;
    }

    public async Task StartTestingAsync()
    {
        if (IsTestingActive)
            return;

        if (_serialService.ConnectionState != Core.Enums.ConnectionState.Connected)
            throw new InvalidOperationException("Arduino not connected");

        // Enable test mode on Arduino (disables keyboard output during testing)
        await _serialService.SendCommandAsync(new SerialCommand
        {
            CommandType = SerialProtocol.Commands.TestMode,
            Parameters = { ["enabled"] = true }
        });

        // Initialize states for all configured inputs
        InitializeInputStates();

        // Start polling loop
        _pollingCts = new CancellationTokenSource();
        _pollingTask = PollInputStatesAsync(_pollingCts.Token);

        IsTestingActive = true;
    }

    public async Task StopTestingAsync()
    {
        if (!IsTestingActive)
            return;

        // Cancel polling
        _pollingCts?.Cancel();
        if (_pollingTask != null)
        {
            try { await _pollingTask; } catch (OperationCanceledException) { }
        }
        _pollingCts?.Dispose();
        _pollingCts = null;

        // Disable test mode on Arduino
        await _serialService.SendCommandAsync(new SerialCommand
        {
            CommandType = SerialProtocol.Commands.TestMode,
            Parameters = { ["enabled"] = false }
        });

        IsTestingActive = false;
    }

    /// <summary>
    /// Initializes input states from current configuration
    /// </summary>
    private void InitializeInputStates()
    {
        lock (_stateLock)
        {
            _currentStates.Clear();

            if (_configService.CurrentConfiguration == null)
                return;

            foreach (var input in _configService.CurrentConfiguration.Inputs)
            {
                _currentStates[input.Id] = new InputState
                {
                    InputId = input.Id,
                    InputName = input.Name,
                    IsActive = false,
                    EncoderValue = input is EncoderConfiguration enc ? enc.CurrentValue : null,
                    LastChanged = DateTime.UtcNow,
                    TriggerCount = 0
                };
            }
        }
    }

    /// <summary>
    /// Main polling loop - requests state from Arduino periodically
    /// </summary>
    private async Task PollInputStatesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _serialService.SendCommandAsync(new SerialCommand
                {
                    CommandType = SerialProtocol.Commands.GetState,
                    TimeoutMs = PollingIntervalMs * 2
                }, cancellationToken);

                if (response.Success && !string.IsNullOrEmpty(response.Data))
                {
                    ProcessStateResponse(response.Data);
                }

                await Task.Delay(PollingIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue polling
                System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
                await Task.Delay(PollingIntervalMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes state response from Arduino
    /// Expected format: {"inputs":[{"id":0,"state":1,"value":100},...]]}
    /// </summary>
    private void ProcessStateResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("inputs", out var inputs))
                return;

            var config = _configService.CurrentConfiguration;
            if (config == null)
                return;

            lock (_stateLock)
            {
                foreach (var inputElement in inputs.EnumerateArray())
                {
                    var inputIndex = inputElement.GetProperty("id").GetInt32();
                    if (inputIndex < 0 || inputIndex >= config.Inputs.Count)
                        continue;

                    var configInput = config.Inputs[inputIndex];
                    if (!_currentStates.TryGetValue(configInput.Id, out var state))
                        continue;

                    var isActive = inputElement.GetProperty("state").GetInt32() == 1;
                    int? value = inputElement.TryGetProperty("value", out var v) ? v.GetInt32() : null;

                    // Detect state changes
                    var wasActive = state.IsActive;
                    state.IsActive = isActive;

                    if (value.HasValue)
                    {
                        state.EncoderValue = value;
                    }

                    if (isActive != wasActive)
                    {
                        state.LastChanged = DateTime.UtcNow;
                        if (isActive)
                        {
                            state.TriggerCount++;
                        }
                    }
                }
            }

            // Raise update event
            RaiseStatesUpdated();
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles event-driven data from Arduino (input triggers)
    /// </summary>
    private void OnSerialDataReceived(object? sender, ArduinoDataReceivedEventArgs e)
    {
        if (!IsTestingActive)
            return;

        try
        {
            using var doc = JsonDocument.Parse(e.Data);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var response))
                return;

            if (response.GetString() != SerialProtocol.Responses.InputEvent)
                return;

            var eventType = root.GetProperty("type").GetString();
            var inputIndex = root.GetProperty("id").GetInt32();
            int? value = root.TryGetProperty("value", out var v) ? v.GetInt32() : null;

            var config = _configService.CurrentConfiguration;
            if (config == null || inputIndex < 0 || inputIndex >= config.Inputs.Count)
                return;

            var configInput = config.Inputs[inputIndex];
            var action = MapEventToAction(eventType);

            InputTriggered?.Invoke(this, new InputTriggeredEventArgs
            {
                InputId = configInput.Id,
                InputName = configInput.Name,
                Action = action,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (JsonException)
        {
            // Not a JSON message or not an input event - ignore
        }
    }

    private static InputAction MapEventToAction(string? eventType)
    {
        return eventType switch
        {
            SerialProtocol.InputEvents.ButtonPressed => InputAction.Press,
            SerialProtocol.InputEvents.ButtonReleased => InputAction.Release,
            SerialProtocol.InputEvents.EncoderCw => InputAction.RotateClockwise,
            SerialProtocol.InputEvents.EncoderCcw => InputAction.RotateCounterClockwise,
            SerialProtocol.InputEvents.EncoderPress => InputAction.EncoderPress,
            SerialProtocol.InputEvents.ToggleOn => InputAction.ToggleOn,
            SerialProtocol.InputEvents.ToggleOff => InputAction.ToggleOff,
            _ => InputAction.Press
        };
    }

    public IReadOnlyDictionary<Guid, InputState> GetCurrentStates()
    {
        lock (_stateLock)
        {
            return new Dictionary<Guid, InputState>(_currentStates);
        }
    }

    public InputState? GetInputState(Guid inputId)
    {
        lock (_stateLock)
        {
            return _currentStates.TryGetValue(inputId, out var state) ? state : null;
        }
    }

    public void SimulateTrigger(Guid inputId, InputAction action)
    {
        var config = _configService.CurrentConfiguration;
        var input = config?.Inputs.FirstOrDefault(i => i.Id == inputId);

        if (input == null)
            return;

        lock (_stateLock)
        {
            if (_currentStates.TryGetValue(inputId, out var state))
            {
                state.IsActive = action is InputAction.Press or InputAction.ToggleOn;
                state.LastChanged = DateTime.UtcNow;
                state.TriggerCount++;

                if (input is EncoderConfiguration && state.EncoderValue.HasValue)
                {
                    if (action == InputAction.RotateClockwise)
                        state.EncoderValue++;
                    else if (action == InputAction.RotateCounterClockwise)
                        state.EncoderValue--;
                }
            }
        }

        InputTriggered?.Invoke(this, new InputTriggeredEventArgs
        {
            InputId = inputId,
            InputName = input.Name,
            Action = action,
            Value = _currentStates.TryGetValue(inputId, out var s) ? s.EncoderValue : null,
            Timestamp = DateTime.UtcNow
        });

        RaiseStatesUpdated();
    }

    public async Task TestDisplayAsync(Guid displayId, DisplayTestPattern pattern)
    {
        await _serialService.SendCommandAsync(new SerialCommand
        {
            CommandType = SerialProtocol.Commands.TestDisplay,
            Parameters =
            {
                ["display"] = GetDisplayIndex(displayId),
                ["pattern"] = pattern.ToString().ToUpperInvariant()
            }
        });
    }

    public async Task SetDisplayValueAsync(Guid displayId, long value)
    {
        var config = _configService.CurrentConfiguration;
        var display = config?.Displays.FirstOrDefault(d => d.Id == displayId);

        if (display == null)
            return;

        await _serialService.SendCommandAsync(new SerialCommand
        {
            CommandType = SerialProtocol.Commands.SetDisplay,
            Parameters =
            {
                ["display"] = GetDisplayIndex(displayId),
                ["value"] = value,
                ["brightness"] = display.Brightness
            }
        });
    }

    private int GetDisplayIndex(Guid displayId)
    {
        var config = _configService.CurrentConfiguration;
        if (config == null)
            return 0;

        return config.Displays.FindIndex(d => d.Id == displayId);
    }

    private void RaiseStatesUpdated()
    {
        Dictionary<Guid, InputState> statesCopy;
        lock (_stateLock)
        {
            statesCopy = new Dictionary<Guid, InputState>(_currentStates);
        }

        InputStatesUpdated?.Invoke(this, new InputStatesUpdatedEventArgs
        {
            States = statesCopy,
            Timestamp = DateTime.UtcNow
        });
    }

    public void Dispose()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _serialService.DataReceived -= OnSerialDataReceived;
    }
}
