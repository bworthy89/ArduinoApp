using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.ViewModels;

/// <summary>
/// ViewModel for the Input Testing page
/// Provides real-time visual feedback for all configured inputs and displays
/// </summary>
public partial class InputTestingViewModel : ObservableObject
{
    private readonly ISerialService _serialService;
    private readonly IConfigurationService _configService;
    private readonly IInputTestingService _testingService;

    [ObservableProperty]
    private bool _isTestingActive;

    [ObservableProperty]
    private ConnectionState _connectionState;

    [ObservableProperty]
    private string _lastEvent = "No events";

    [ObservableProperty]
    private int _eventCount;

    public ObservableCollection<InputStateViewModel> InputStates { get; } = [];
    public ObservableCollection<DisplayStateViewModel> DisplayStates { get; } = [];
    public ObservableCollection<string> EventLog { get; } = [];

    public InputTestingViewModel(
        ISerialService serialService,
        IConfigurationService configService,
        IInputTestingService testingService)
    {
        _serialService = serialService;
        _configService = configService;
        _testingService = testingService;

        _serialService.ConnectionStateChanged += OnConnectionStateChanged;
        _testingService.InputStatesUpdated += OnInputStatesUpdated;
        _testingService.InputTriggered += OnInputTriggered;
        _configService.ConfigurationChanged += OnConfigurationChanged;

        RefreshFromConfiguration();
    }

    [RelayCommand]
    private async Task StartTestingAsync()
    {
        if (ConnectionState != ConnectionState.Connected)
        {
            AddEvent("Error: Arduino not connected");
            return;
        }

        try
        {
            await _testingService.StartTestingAsync();
            IsTestingActive = true;
            AddEvent("Testing started");
        }
        catch (Exception ex)
        {
            AddEvent($"Error starting test: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopTestingAsync()
    {
        await _testingService.StopTestingAsync();
        IsTestingActive = false;
        AddEvent("Testing stopped");
    }

    [RelayCommand]
    private void ClearEventLog()
    {
        EventLog.Clear();
        EventCount = 0;
        LastEvent = "No events";
    }

    [RelayCommand]
    private async Task TestDisplayAsync(Guid displayId)
    {
        if (ConnectionState != ConnectionState.Connected)
            return;

        await _testingService.TestDisplayAsync(displayId, DisplayTestPattern.CountUp);
        AddEvent($"Testing display");
    }

    [RelayCommand]
    private async Task SetDisplayValueAsync(DisplayStateViewModel displayState)
    {
        if (ConnectionState != ConnectionState.Connected)
            return;

        await _testingService.SetDisplayValueAsync(displayState.DisplayId, displayState.Value);
    }

    [RelayCommand]
    private void SimulateInput(InputStateViewModel inputState)
    {
        _testingService.SimulateTrigger(inputState.InputId, InputAction.Press);
    }

    private void RefreshFromConfiguration()
    {
        InputStates.Clear();
        DisplayStates.Clear();

        var config = _configService.CurrentConfiguration;
        if (config == null)
            return;

        foreach (var input in config.Inputs)
        {
            InputStates.Add(new InputStateViewModel
            {
                InputId = input.Id,
                Name = input.Name,
                InputType = input.InputType,
                IsActive = false,
                Value = input is EncoderConfiguration enc ? enc.CurrentValue : 0
            });
        }

        foreach (var display in config.Displays)
        {
            DisplayStates.Add(new DisplayStateViewModel
            {
                DisplayId = display.Id,
                Name = display.Name,
                Value = display.CurrentValue,
                Brightness = display.Brightness
            });
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        ConnectionState = e.NewState;

        if (e.NewState != ConnectionState.Connected && IsTestingActive)
        {
            IsTestingActive = false;
            AddEvent("Testing stopped: Disconnected");
        }
    }

    private void OnInputStatesUpdated(object? sender, InputStatesUpdatedEventArgs e)
    {
        foreach (var state in e.States)
        {
            var viewModel = InputStates.FirstOrDefault(i => i.InputId == state.Key);
            if (viewModel != null)
            {
                viewModel.IsActive = state.Value.IsActive;
                viewModel.Value = state.Value.EncoderValue ?? viewModel.Value;
                viewModel.TriggerCount = state.Value.TriggerCount;
            }
        }
    }

    private void OnInputTriggered(object? sender, InputTriggeredEventArgs e)
    {
        var actionText = e.Action switch
        {
            InputAction.Press => "Pressed",
            InputAction.Release => "Released",
            InputAction.RotateClockwise => $"CW → {e.Value}",
            InputAction.RotateCounterClockwise => $"CCW → {e.Value}",
            InputAction.EncoderPress => "Button",
            InputAction.ToggleOn => "ON",
            InputAction.ToggleOff => "OFF",
            _ => e.Action.ToString()
        };

        var eventText = $"[{e.Timestamp:HH:mm:ss.fff}] {e.InputName}: {actionText}";
        AddEvent(eventText);

        // Update linked displays
        var viewModel = InputStates.FirstOrDefault(i => i.InputId == e.InputId);
        if (viewModel != null)
        {
            viewModel.LastAction = actionText;
            viewModel.LastActionTime = e.Timestamp;
        }
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        RefreshFromConfiguration();
    }

    private void AddEvent(string eventText)
    {
        EventLog.Insert(0, eventText);
        if (EventLog.Count > 100)
        {
            EventLog.RemoveAt(EventLog.Count - 1);
        }

        LastEvent = eventText;
        EventCount++;
    }
}

/// <summary>
/// View model for individual input state in the testing panel
/// </summary>
public partial class InputStateViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _inputId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private InputType _inputType;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private int _triggerCount;

    [ObservableProperty]
    private string _lastAction = string.Empty;

    [ObservableProperty]
    private DateTime _lastActionTime;

    /// <summary>
    /// Visual indicator color based on state
    /// </summary>
    public string StateColor => IsActive ? "#27ae60" : "#95a5a6";
}

/// <summary>
/// View model for individual display state in the testing panel
/// </summary>
public partial class DisplayStateViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _displayId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private long _value;

    [ObservableProperty]
    private int _brightness = 8;

    /// <summary>
    /// Formatted value for display (8 digits with leading zeros if needed)
    /// </summary>
    public string FormattedValue => Value.ToString("D8");
}
