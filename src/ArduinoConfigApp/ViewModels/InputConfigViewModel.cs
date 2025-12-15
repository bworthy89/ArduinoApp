using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.ViewModels;

/// <summary>
/// ViewModel for the Input Configuration page
/// Allows adding, editing, and removing input components
/// </summary>
public partial class InputConfigViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IInputTestingService _testingService;

    [ObservableProperty]
    private InputConfiguration? _selectedInput;

    [ObservableProperty]
    private InputType _newInputType = InputType.MomentaryButton;

    [ObservableProperty]
    private string _newInputName = string.Empty;

    [ObservableProperty]
    private int _newInputPin;

    [ObservableProperty]
    private int _newEncoderPinA;

    [ObservableProperty]
    private int _newEncoderPinB;

    [ObservableProperty]
    private int _newEncoderButtonPin = -1;

    [ObservableProperty]
    private bool _useInternalPullup = true;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isAddingInput;

    public ObservableCollection<InputConfiguration> Inputs { get; } = [];

    public ObservableCollection<int> AvailablePins { get; } = [];

    public DashboardViewModel(IConfigurationService configService, IInputTestingService testingService)
    {
        _configService = configService;
        _testingService = testingService;

        _configService.ConfigurationChanged += OnConfigurationChanged;
        RefreshInputs();
    }

    // Constructor overload for design-time
    public InputConfigViewModel(IConfigurationService configService, IInputTestingService testingService)
    {
        _configService = configService;
        _testingService = testingService;

        _configService.ConfigurationChanged += OnConfigurationChanged;
        RefreshInputs();
    }

    [RelayCommand]
    private void StartAddInput()
    {
        IsAddingInput = true;
        NewInputName = $"Input {Inputs.Count + 1}";
        ValidationError = null;
        RefreshAvailablePins();
    }

    [RelayCommand]
    private void CancelAddInput()
    {
        IsAddingInput = false;
        NewInputName = string.Empty;
        ValidationError = null;
    }

    [RelayCommand]
    private void AddInput()
    {
        if (string.IsNullOrWhiteSpace(NewInputName))
        {
            ValidationError = "Input name is required";
            return;
        }

        InputConfiguration newInput;

        try
        {
            newInput = NewInputType switch
            {
                InputType.MomentaryButton => new ButtonConfiguration
                {
                    Name = NewInputName,
                    Pin = NewInputPin,
                    IsLatching = false,
                    UseInternalPullup = UseInternalPullup
                },
                InputType.LatchingButton => new ButtonConfiguration
                {
                    Name = NewInputName,
                    Pin = NewInputPin,
                    IsLatching = true,
                    UseInternalPullup = UseInternalPullup
                },
                InputType.RotaryEncoder => new EncoderConfiguration
                {
                    Name = NewInputName,
                    PinA = NewEncoderPinA,
                    PinB = NewEncoderPinB,
                    ButtonPin = NewEncoderButtonPin,
                    UseInternalPullups = UseInternalPullup
                },
                InputType.ToggleSwitch => new ToggleSwitchConfiguration
                {
                    Name = NewInputName,
                    Pin = NewInputPin,
                    UseInternalPullup = UseInternalPullup
                },
                _ => throw new ArgumentOutOfRangeException()
            };

            _configService.AddInput(newInput);
            IsAddingInput = false;
            NewInputName = string.Empty;
            ValidationError = null;
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    [RelayCommand]
    private void RemoveInput(InputConfiguration input)
    {
        _configService.RemoveInput(input.Id);
    }

    [RelayCommand]
    private void SelectInput(InputConfiguration input)
    {
        SelectedInput = input;
    }

    [RelayCommand]
    private void DuplicateInput(InputConfiguration input)
    {
        InputConfiguration duplicate = input switch
        {
            ButtonConfiguration btn => new ButtonConfiguration
            {
                Name = $"{btn.Name} (Copy)",
                Pin = GetNextAvailablePin(),
                IsLatching = btn.IsLatching,
                UseInternalPullup = btn.UseInternalPullup,
                DebounceMs = btn.DebounceMs
            },
            EncoderConfiguration enc => new EncoderConfiguration
            {
                Name = $"{enc.Name} (Copy)",
                PinA = GetNextAvailablePin(),
                PinB = GetNextAvailablePin(),
                ButtonPin = enc.ButtonPin >= 0 ? GetNextAvailablePin() : -1,
                UseInternalPullups = enc.UseInternalPullups,
                Increment = enc.Increment
            },
            ToggleSwitchConfiguration tog => new ToggleSwitchConfiguration
            {
                Name = $"{tog.Name} (Copy)",
                Pin = GetNextAvailablePin(),
                UseInternalPullup = tog.UseInternalPullup,
                DebounceMs = tog.DebounceMs
            },
            _ => throw new NotSupportedException()
        };

        try
        {
            _configService.AddInput(duplicate);
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task TestInputAsync(InputConfiguration input)
    {
        if (!_testingService.IsTestingActive)
        {
            await _testingService.StartTestingAsync();
        }

        // The testing service will send events when this input is triggered
        _testingService.SimulateTrigger(input.Id, InputAction.Press);
    }

    [RelayCommand]
    private void UpdateInput()
    {
        if (SelectedInput == null)
            return;

        try
        {
            _configService.UpdateInput(SelectedInput);
            ValidationError = null;
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    private void RefreshInputs()
    {
        Inputs.Clear();

        if (_configService.CurrentConfiguration == null)
            return;

        foreach (var input in _configService.CurrentConfiguration.Inputs)
        {
            Inputs.Add(input);
        }

        RefreshAvailablePins();
    }

    private void RefreshAvailablePins()
    {
        AvailablePins.Clear();

        if (_configService.CurrentConfiguration == null)
            return;

        var board = new ArduinoBoard { BoardType = _configService.CurrentConfiguration.TargetBoard };
        var usedPins = _configService.GetUsedPins();

        foreach (var pin in board.AvailablePins)
        {
            if (!usedPins.Contains(pin.PinNumber))
            {
                AvailablePins.Add(pin.PinNumber);
            }
        }

        // Set defaults to first available pins
        if (AvailablePins.Count > 0)
        {
            NewInputPin = AvailablePins[0];
            NewEncoderPinA = AvailablePins[0];
            NewEncoderPinB = AvailablePins.Count > 1 ? AvailablePins[1] : AvailablePins[0];
        }
    }

    private int GetNextAvailablePin()
    {
        if (_configService.CurrentConfiguration == null)
            return 2;

        var board = new ArduinoBoard { BoardType = _configService.CurrentConfiguration.TargetBoard };
        var usedPins = _configService.GetUsedPins();

        foreach (var pin in board.AvailablePins)
        {
            if (!usedPins.Contains(pin.PinNumber))
            {
                return pin.PinNumber;
            }
        }

        return -1;
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        RefreshInputs();
    }
}
