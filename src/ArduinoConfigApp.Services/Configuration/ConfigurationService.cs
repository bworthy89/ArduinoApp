using System.Text.Json;
using System.Text.Json.Serialization;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Services.Configuration;

/// <summary>
/// Service for managing project configurations with JSON persistence
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ProjectConfiguration? CurrentConfiguration { get; private set; }
    public bool HasUnsavedChanges { get; private set; }
    public string? CurrentFilePath { get; private set; }

    public ConfigurationService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(),
                new InputConfigurationJsonConverter()
            }
        };
    }

    public ProjectConfiguration CreateNew(string name)
    {
        CurrentConfiguration = new ProjectConfiguration
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        CurrentFilePath = null;
        HasUnsavedChanges = true;

        RaiseConfigurationChanged(ConfigurationChangeType.NewConfiguration, CurrentConfiguration);
        return CurrentConfiguration;
    }

    public async Task<ProjectConfiguration> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        CurrentConfiguration = JsonSerializer.Deserialize<ProjectConfiguration>(json, _jsonOptions)
            ?? throw new InvalidDataException("Failed to deserialize configuration");

        CurrentFilePath = filePath;
        HasUnsavedChanges = false;

        RaiseConfigurationChanged(ConfigurationChangeType.ConfigurationLoaded, CurrentConfiguration);
        return CurrentConfiguration;
    }

    public async Task SaveAsync(string filePath)
    {
        EnsureConfigurationExists();

        CurrentConfiguration!.ModifiedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(CurrentConfiguration, _jsonOptions);

        await File.WriteAllTextAsync(filePath, json);

        CurrentFilePath = filePath;
        HasUnsavedChanges = false;
    }

    public Task SaveAsync()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
            throw new InvalidOperationException("No file path set. Use SaveAsync(filePath) instead.");

        return SaveAsync(CurrentFilePath);
    }

    public ConfigurationValidationResult Validate()
    {
        EnsureConfigurationExists();
        return CurrentConfiguration!.Validate();
    }

    public void AddInput(InputConfiguration input)
    {
        EnsureConfigurationExists();

        // Validate pin availability
        foreach (var pin in input.UsedPins)
        {
            if (!IsPinAvailable(pin))
                throw new InvalidOperationException($"Pin {pin} is already in use");
        }

        CurrentConfiguration!.Inputs.Add(input);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.InputAdded, input);
    }

    public void RemoveInput(Guid inputId)
    {
        EnsureConfigurationExists();

        var input = CurrentConfiguration!.Inputs.FirstOrDefault(i => i.Id == inputId);
        if (input == null) return;

        // Remove associated mappings
        CurrentConfiguration.KeyboardMappings.RemoveAll(m => m.InputId == inputId);

        // Remove encoder display links
        if (input is EncoderConfiguration encoder && encoder.LinkedDisplayId.HasValue)
        {
            var display = CurrentConfiguration.Displays.FirstOrDefault(d => d.Id == encoder.LinkedDisplayId);
            display?.LinkedEncoderIds.Remove(inputId);
        }

        CurrentConfiguration.Inputs.Remove(input);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.InputRemoved, input);
    }

    public void UpdateInput(InputConfiguration input)
    {
        EnsureConfigurationExists();

        var index = CurrentConfiguration!.Inputs.FindIndex(i => i.Id == input.Id);
        if (index < 0)
            throw new InvalidOperationException($"Input with ID {input.Id} not found");

        // Validate pin availability (excluding current input)
        foreach (var pin in input.UsedPins)
        {
            if (!IsPinAvailable(pin, input.Id))
                throw new InvalidOperationException($"Pin {pin} is already in use");
        }

        CurrentConfiguration.Inputs[index] = input;
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.InputUpdated, input);
    }

    public void AddDisplay(DisplayConfiguration display)
    {
        EnsureConfigurationExists();

        // Check CS pin conflicts
        if (CurrentConfiguration!.Displays.Any(d => d.CsPin == display.CsPin))
            throw new InvalidOperationException($"CS pin {display.CsPin} is already used by another display");

        CurrentConfiguration.Displays.Add(display);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.DisplayAdded, display);
    }

    public void RemoveDisplay(Guid displayId)
    {
        EnsureConfigurationExists();

        var display = CurrentConfiguration!.Displays.FirstOrDefault(d => d.Id == displayId);
        if (display == null) return;

        // Unlink all encoders
        foreach (var encoderId in display.LinkedEncoderIds.ToList())
        {
            var encoder = CurrentConfiguration.Inputs.OfType<EncoderConfiguration>()
                .FirstOrDefault(e => e.Id == encoderId);
            if (encoder != null)
            {
                encoder.LinkedDisplayId = null;
            }
        }

        CurrentConfiguration.Displays.Remove(display);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.DisplayRemoved, display);
    }

    public void UpdateDisplay(DisplayConfiguration display)
    {
        EnsureConfigurationExists();

        var index = CurrentConfiguration!.Displays.FindIndex(d => d.Id == display.Id);
        if (index < 0)
            throw new InvalidOperationException($"Display with ID {display.Id} not found");

        CurrentConfiguration.Displays[index] = display;
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.DisplayUpdated, display);
    }

    public void AddMapping(KeyboardMapping mapping)
    {
        EnsureConfigurationExists();

        // Verify input exists
        if (!CurrentConfiguration!.Inputs.Any(i => i.Id == mapping.InputId))
            throw new InvalidOperationException("Input not found for mapping");

        CurrentConfiguration.KeyboardMappings.Add(mapping);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.MappingAdded, mapping);
    }

    public void RemoveMapping(Guid mappingId)
    {
        EnsureConfigurationExists();

        var mapping = CurrentConfiguration!.KeyboardMappings.FirstOrDefault(m => m.Id == mappingId);
        if (mapping == null) return;

        CurrentConfiguration.KeyboardMappings.Remove(mapping);
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.MappingRemoved, mapping);
    }

    public void UpdateMapping(KeyboardMapping mapping)
    {
        EnsureConfigurationExists();

        var index = CurrentConfiguration!.KeyboardMappings.FindIndex(m => m.Id == mapping.Id);
        if (index < 0)
            throw new InvalidOperationException($"Mapping with ID {mapping.Id} not found");

        CurrentConfiguration.KeyboardMappings[index] = mapping;
        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.MappingUpdated, mapping);
    }

    public void LinkEncoderToDisplay(Guid encoderId, Guid displayId)
    {
        EnsureConfigurationExists();

        var encoder = CurrentConfiguration!.Inputs.OfType<EncoderConfiguration>()
            .FirstOrDefault(e => e.Id == encoderId)
            ?? throw new InvalidOperationException("Encoder not found");

        var display = CurrentConfiguration.Displays.FirstOrDefault(d => d.Id == displayId)
            ?? throw new InvalidOperationException("Display not found");

        // Remove from previous display if linked
        if (encoder.LinkedDisplayId.HasValue)
        {
            var oldDisplay = CurrentConfiguration.Displays.FirstOrDefault(d => d.Id == encoder.LinkedDisplayId);
            oldDisplay?.LinkedEncoderIds.Remove(encoderId);
        }

        encoder.LinkedDisplayId = displayId;
        if (!display.LinkedEncoderIds.Contains(encoderId))
        {
            display.LinkedEncoderIds.Add(encoderId);
        }

        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.EncoderLinked, new { encoderId, displayId });
    }

    public void UnlinkEncoderFromDisplay(Guid encoderId, Guid displayId)
    {
        EnsureConfigurationExists();

        var encoder = CurrentConfiguration!.Inputs.OfType<EncoderConfiguration>()
            .FirstOrDefault(e => e.Id == encoderId);

        var display = CurrentConfiguration.Displays.FirstOrDefault(d => d.Id == displayId);

        if (encoder != null)
        {
            encoder.LinkedDisplayId = null;
        }

        display?.LinkedEncoderIds.Remove(encoderId);

        MarkModified();
        RaiseConfigurationChanged(ConfigurationChangeType.EncoderUnlinked, new { encoderId, displayId });
    }

    public IReadOnlyList<int> GetUsedPins()
    {
        if (CurrentConfiguration == null)
            return [];

        var pins = new HashSet<int>();

        foreach (var input in CurrentConfiguration.Inputs)
        {
            foreach (var pin in input.UsedPins)
            {
                pins.Add(pin);
            }
        }

        foreach (var display in CurrentConfiguration.Displays)
        {
            pins.Add(display.CsPin);
        }

        return pins.OrderBy(p => p).ToList();
    }

    public bool IsPinAvailable(int pin, Guid? excludeInputId = null)
    {
        if (CurrentConfiguration == null)
            return true;

        // Check inputs
        foreach (var input in CurrentConfiguration.Inputs)
        {
            if (excludeInputId.HasValue && input.Id == excludeInputId.Value)
                continue;

            if (input.UsedPins.Contains(pin))
                return false;
        }

        // Check display CS pins
        foreach (var display in CurrentConfiguration.Displays)
        {
            if (display.CsPin == pin)
                return false;
        }

        return true;
    }

    public string ExportToJson()
    {
        EnsureConfigurationExists();
        return JsonSerializer.Serialize(CurrentConfiguration, _jsonOptions);
    }

    public ProjectConfiguration ImportFromJson(string json)
    {
        return JsonSerializer.Deserialize<ProjectConfiguration>(json, _jsonOptions)
            ?? throw new InvalidDataException("Failed to deserialize configuration");
    }

    private void EnsureConfigurationExists()
    {
        if (CurrentConfiguration == null)
            throw new InvalidOperationException("No configuration loaded. Create or load a configuration first.");
    }

    private void MarkModified()
    {
        HasUnsavedChanges = true;
        if (CurrentConfiguration != null)
        {
            CurrentConfiguration.ModifiedAt = DateTime.UtcNow;
        }
    }

    private void RaiseConfigurationChanged(ConfigurationChangeType changeType, object? changedItem)
    {
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            ChangeType = changeType,
            ChangedItem = changedItem
        });
    }
}

/// <summary>
/// Custom JSON converter for polymorphic InputConfiguration deserialization
/// </summary>
public class InputConfigurationJsonConverter : JsonConverter<InputConfiguration>
{
    public override InputConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("inputType", out var typeProperty))
        {
            throw new JsonException("InputConfiguration must have an inputType property");
        }

        var inputType = typeProperty.GetString();
        var json = root.GetRawText();

        return inputType switch
        {
            "LatchingButton" or "MomentaryButton" => JsonSerializer.Deserialize<ButtonConfiguration>(json, options),
            "RotaryEncoder" => JsonSerializer.Deserialize<EncoderConfiguration>(json, options),
            "ToggleSwitch" => JsonSerializer.Deserialize<ToggleSwitchConfiguration>(json, options),
            _ => throw new JsonException($"Unknown input type: {inputType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, InputConfiguration value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
