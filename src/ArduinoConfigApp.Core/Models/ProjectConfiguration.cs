using ArduinoConfigApp.Core.Enums;

namespace ArduinoConfigApp.Core.Models;

/// <summary>
/// Complete project configuration containing all settings
/// This is the root object that gets saved/loaded from JSON
/// </summary>
public class ProjectConfiguration
{
    /// <summary>
    /// Unique identifier for this project
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; set; } = "New Project";

    /// <summary>
    /// Project description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Configuration file version for compatibility
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// When the project was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the project was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Target Arduino board type
    /// </summary>
    public BoardType TargetBoard { get; set; } = BoardType.ProMicro;

    /// <summary>
    /// All configured inputs
    /// </summary>
    public List<InputConfiguration> Inputs { get; set; } = [];

    /// <summary>
    /// All configured displays
    /// </summary>
    public List<DisplayConfiguration> Displays { get; set; } = [];

    /// <summary>
    /// All keyboard mappings
    /// </summary>
    public List<KeyboardMapping> KeyboardMappings { get; set; } = [];

    /// <summary>
    /// All macro definitions
    /// </summary>
    public List<KeyboardMacro> Macros { get; set; } = [];

    /// <summary>
    /// Validates the configuration for conflicts and errors
    /// </summary>
    public ConfigurationValidationResult Validate()
    {
        var result = new ConfigurationValidationResult();

        // Check for duplicate pin assignments
        var allPins = new Dictionary<int, string>();
        foreach (var input in Inputs)
        {
            foreach (var pin in input.UsedPins)
            {
                if (allPins.TryGetValue(pin, out var existingInput))
                {
                    result.Errors.Add($"Pin {pin} is used by both '{existingInput}' and '{input.Name}'");
                }
                else
                {
                    allPins[pin] = input.Name;
                }
            }
        }

        // Check display CS pins
        foreach (var display in Displays)
        {
            if (allPins.TryGetValue(display.CsPin, out var existingInput))
            {
                result.Errors.Add($"Display '{display.Name}' CS pin {display.CsPin} conflicts with input '{existingInput}'");
            }
        }

        // Check for displays with multiple encoders (warning, not error)
        foreach (var display in Displays)
        {
            if (display.LinkedEncoderIds.Count > 1)
            {
                result.Warnings.Add($"Display '{display.Name}' has multiple encoders linked. Consider conflict resolution rules.");
            }
        }

        // Check for inputs without keyboard mappings (warning)
        foreach (var input in Inputs)
        {
            var hasMapping = KeyboardMappings.Any(m => m.InputId == input.Id);
            if (!hasMapping)
            {
                result.Warnings.Add($"Input '{input.Name}' has no keyboard mapping assigned.");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

/// <summary>
/// Result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
