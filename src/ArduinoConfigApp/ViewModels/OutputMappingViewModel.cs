using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.ViewModels;

/// <summary>
/// ViewModel for the Output Mapping page
/// Maps inputs to keyboard outputs with support for combinations and special keys
/// </summary>
public partial class OutputMappingViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private InputConfiguration? _selectedInput;

    [ObservableProperty]
    private KeyboardMapping? _selectedMapping;

    [ObservableProperty]
    private InputAction _newMappingAction = InputAction.Press;

    [ObservableProperty]
    private KeyboardKey _newMappingKey = KeyboardKey.A;

    [ObservableProperty]
    private bool _ctrlModifier;

    [ObservableProperty]
    private bool _shiftModifier;

    [ObservableProperty]
    private bool _altModifier;

    [ObservableProperty]
    private bool _winModifier;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isAddingMapping;

    [ObservableProperty]
    private string _previewText = "Press a key...";

    public ObservableCollection<InputConfiguration> Inputs { get; } = [];
    public ObservableCollection<KeyboardMapping> Mappings { get; } = [];
    public ObservableCollection<KeyboardMappingGroup> MappingGroups { get; } = [];

    // Available options for UI
    public static IReadOnlyList<InputAction> AvailableActions { get; } =
    [
        InputAction.Press,
        InputAction.Release,
        InputAction.Hold,
        InputAction.RotateClockwise,
        InputAction.RotateCounterClockwise,
        InputAction.EncoderPress,
        InputAction.ToggleOn,
        InputAction.ToggleOff
    ];

    public static IReadOnlyList<KeyboardKey> AvailableKeys { get; } =
        Enum.GetValues<KeyboardKey>().ToList();

    public OutputMappingViewModel(IConfigurationService configService)
    {
        _configService = configService;
        _configService.ConfigurationChanged += OnConfigurationChanged;
        RefreshData();
    }

    [RelayCommand]
    private void SelectInput(InputConfiguration input)
    {
        SelectedInput = input;
        RefreshMappingsForInput();
    }

    [RelayCommand]
    private void StartAddMapping()
    {
        if (SelectedInput == null)
        {
            ValidationError = "Select an input first";
            return;
        }

        IsAddingMapping = true;
        ValidationError = null;

        // Set default action based on input type
        NewMappingAction = SelectedInput.InputType switch
        {
            InputType.RotaryEncoder => InputAction.RotateClockwise,
            InputType.ToggleSwitch => InputAction.ToggleOn,
            _ => InputAction.Press
        };

        UpdatePreview();
    }

    [RelayCommand]
    private void CancelAddMapping()
    {
        IsAddingMapping = false;
        ValidationError = null;
        ResetModifiers();
    }

    [RelayCommand]
    private void AddMapping()
    {
        if (SelectedInput == null)
        {
            ValidationError = "Select an input first";
            return;
        }

        var modifiers = ModifierKeys.None;
        if (CtrlModifier) modifiers |= ModifierKeys.Ctrl;
        if (ShiftModifier) modifiers |= ModifierKeys.Shift;
        if (AltModifier) modifiers |= ModifierKeys.Alt;
        if (WinModifier) modifiers |= ModifierKeys.Gui;

        var mapping = new KeyboardMapping
        {
            InputId = SelectedInput.Id,
            TriggerAction = NewMappingAction,
            Key = NewMappingKey,
            Modifiers = modifiers,
            HoldWhileActive = NewMappingAction == InputAction.ToggleOn
        };

        try
        {
            _configService.AddMapping(mapping);
            IsAddingMapping = false;
            ResetModifiers();
            ValidationError = null;
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    [RelayCommand]
    private void RemoveMapping(KeyboardMapping mapping)
    {
        _configService.RemoveMapping(mapping.Id);
    }

    [RelayCommand]
    private void SelectMapping(KeyboardMapping mapping)
    {
        SelectedMapping = mapping;
    }

    [RelayCommand]
    private void UpdateMapping()
    {
        if (SelectedMapping == null)
            return;

        try
        {
            _configService.UpdateMapping(SelectedMapping);
            ValidationError = null;
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    [RelayCommand]
    private void ToggleMapping(KeyboardMapping mapping)
    {
        mapping.IsEnabled = !mapping.IsEnabled;
        _configService.UpdateMapping(mapping);
    }

    [RelayCommand]
    private void SetKey(KeyboardKey key)
    {
        NewMappingKey = key;
        UpdatePreview();
    }

    [RelayCommand]
    private void ToggleModifier(string modifier)
    {
        switch (modifier.ToLower())
        {
            case "ctrl":
                CtrlModifier = !CtrlModifier;
                break;
            case "shift":
                ShiftModifier = !ShiftModifier;
                break;
            case "alt":
                AltModifier = !AltModifier;
                break;
            case "win":
                WinModifier = !WinModifier;
                break;
        }
        UpdatePreview();
    }

    [RelayCommand]
    private void CreateEncoderPair()
    {
        // Creates CW and CCW mappings for an encoder (common pattern)
        if (SelectedInput is not EncoderConfiguration)
        {
            ValidationError = "Select a rotary encoder first";
            return;
        }

        // Create CW mapping
        var cwMapping = new KeyboardMapping
        {
            InputId = SelectedInput.Id,
            TriggerAction = InputAction.RotateClockwise,
            Key = KeyboardKey.RightArrow,
            Modifiers = ModifierKeys.None
        };

        // Create CCW mapping
        var ccwMapping = new KeyboardMapping
        {
            InputId = SelectedInput.Id,
            TriggerAction = InputAction.RotateCounterClockwise,
            Key = KeyboardKey.LeftArrow,
            Modifiers = ModifierKeys.None
        };

        try
        {
            _configService.AddMapping(cwMapping);
            _configService.AddMapping(ccwMapping);
            ValidationError = null;
        }
        catch (InvalidOperationException ex)
        {
            ValidationError = ex.Message;
        }
    }

    private void RefreshData()
    {
        Inputs.Clear();
        MappingGroups.Clear();

        var config = _configService.CurrentConfiguration;
        if (config == null)
            return;

        foreach (var input in config.Inputs)
        {
            Inputs.Add(input);

            // Group mappings by input
            var inputMappings = config.KeyboardMappings.Where(m => m.InputId == input.Id).ToList();
            if (inputMappings.Count > 0)
            {
                MappingGroups.Add(new KeyboardMappingGroup
                {
                    Input = input,
                    Mappings = new ObservableCollection<KeyboardMapping>(inputMappings)
                });
            }
        }
    }

    private void RefreshMappingsForInput()
    {
        Mappings.Clear();

        if (SelectedInput == null || _configService.CurrentConfiguration == null)
            return;

        var inputMappings = _configService.CurrentConfiguration.KeyboardMappings
            .Where(m => m.InputId == SelectedInput.Id);

        foreach (var mapping in inputMappings)
        {
            Mappings.Add(mapping);
        }
    }

    private void ResetModifiers()
    {
        CtrlModifier = false;
        ShiftModifier = false;
        AltModifier = false;
        WinModifier = false;
    }

    private void UpdatePreview()
    {
        var parts = new List<string>();

        if (CtrlModifier) parts.Add("Ctrl");
        if (ShiftModifier) parts.Add("Shift");
        if (AltModifier) parts.Add("Alt");
        if (WinModifier) parts.Add("Win");
        parts.Add(NewMappingKey.ToString());

        PreviewText = string.Join(" + ", parts);
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        RefreshData();

        if (SelectedInput != null)
        {
            RefreshMappingsForInput();
        }
    }

    /// <summary>
    /// Gets available actions for the selected input type
    /// </summary>
    public IReadOnlyList<InputAction> GetAvailableActionsForInput()
    {
        if (SelectedInput == null)
            return AvailableActions;

        return SelectedInput.InputType switch
        {
            InputType.MomentaryButton => [InputAction.Press, InputAction.Release, InputAction.Hold],
            InputType.LatchingButton => [InputAction.Press, InputAction.Release],
            InputType.RotaryEncoder => [InputAction.RotateClockwise, InputAction.RotateCounterClockwise, InputAction.EncoderPress],
            InputType.ToggleSwitch => [InputAction.ToggleOn, InputAction.ToggleOff],
            _ => AvailableActions
        };
    }
}

/// <summary>
/// Groups mappings by their input for display
/// </summary>
public class KeyboardMappingGroup
{
    public InputConfiguration Input { get; set; } = null!;
    public ObservableCollection<KeyboardMapping> Mappings { get; set; } = [];
}
