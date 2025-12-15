using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Core.Interfaces;

/// <summary>
/// Service for managing project configurations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Current project configuration
    /// </summary>
    ProjectConfiguration? CurrentConfiguration { get; }

    /// <summary>
    /// Whether there are unsaved changes
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Path to the current configuration file
    /// </summary>
    string? CurrentFilePath { get; }

    /// <summary>
    /// Creates a new empty configuration
    /// </summary>
    ProjectConfiguration CreateNew(string name);

    /// <summary>
    /// Loads a configuration from a file
    /// </summary>
    Task<ProjectConfiguration> LoadAsync(string filePath);

    /// <summary>
    /// Saves the current configuration to a file
    /// </summary>
    Task SaveAsync(string filePath);

    /// <summary>
    /// Saves to the current file path
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Validates the current configuration
    /// </summary>
    ConfigurationValidationResult Validate();

    /// <summary>
    /// Adds an input to the configuration
    /// </summary>
    void AddInput(InputConfiguration input);

    /// <summary>
    /// Removes an input from the configuration
    /// </summary>
    void RemoveInput(Guid inputId);

    /// <summary>
    /// Updates an existing input
    /// </summary>
    void UpdateInput(InputConfiguration input);

    /// <summary>
    /// Adds a display to the configuration
    /// </summary>
    void AddDisplay(DisplayConfiguration display);

    /// <summary>
    /// Removes a display from the configuration
    /// </summary>
    void RemoveDisplay(Guid displayId);

    /// <summary>
    /// Updates an existing display
    /// </summary>
    void UpdateDisplay(DisplayConfiguration display);

    /// <summary>
    /// Adds a keyboard mapping
    /// </summary>
    void AddMapping(KeyboardMapping mapping);

    /// <summary>
    /// Removes a keyboard mapping
    /// </summary>
    void RemoveMapping(Guid mappingId);

    /// <summary>
    /// Updates an existing mapping
    /// </summary>
    void UpdateMapping(KeyboardMapping mapping);

    /// <summary>
    /// Links an encoder to a display
    /// </summary>
    void LinkEncoderToDisplay(Guid encoderId, Guid displayId);

    /// <summary>
    /// Unlinks an encoder from a display
    /// </summary>
    void UnlinkEncoderFromDisplay(Guid encoderId, Guid displayId);

    /// <summary>
    /// Gets all pins currently in use
    /// </summary>
    IReadOnlyList<int> GetUsedPins();

    /// <summary>
    /// Checks if a pin is available
    /// </summary>
    bool IsPinAvailable(int pin, Guid? excludeInputId = null);

    /// <summary>
    /// Exports configuration to JSON string
    /// </summary>
    string ExportToJson();

    /// <summary>
    /// Imports configuration from JSON string
    /// </summary>
    ProjectConfiguration ImportFromJson(string json);
}

/// <summary>
/// Event args for configuration changes
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChangeType ChangeType { get; init; }
    public object? ChangedItem { get; init; }
}

/// <summary>
/// Types of configuration changes
/// </summary>
public enum ConfigurationChangeType
{
    NewConfiguration,
    ConfigurationLoaded,
    InputAdded,
    InputRemoved,
    InputUpdated,
    DisplayAdded,
    DisplayRemoved,
    DisplayUpdated,
    MappingAdded,
    MappingRemoved,
    MappingUpdated,
    EncoderLinked,
    EncoderUnlinked
}
