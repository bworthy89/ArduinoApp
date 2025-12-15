using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Core.Interfaces;

/// <summary>
/// Service for generating Arduino sketches from configuration
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// Generates the complete Arduino sketch for the configuration
    /// </summary>
    GeneratedCode GenerateSketch(ProjectConfiguration configuration);

    /// <summary>
    /// Generates only the pin setup code
    /// </summary>
    string GeneratePinSetup(ProjectConfiguration configuration);

    /// <summary>
    /// Generates only the input handling code
    /// </summary>
    string GenerateInputHandling(ProjectConfiguration configuration);

    /// <summary>
    /// Generates only the display control code
    /// </summary>
    string GenerateDisplayControl(ProjectConfiguration configuration);

    /// <summary>
    /// Generates only the keyboard output code
    /// </summary>
    string GenerateKeyboardOutput(ProjectConfiguration configuration);

    /// <summary>
    /// Gets the required Arduino libraries for the configuration
    /// </summary>
    IReadOnlyList<ArduinoLibrary> GetRequiredLibraries(ProjectConfiguration configuration);

    /// <summary>
    /// Saves the generated sketch to a file
    /// </summary>
    Task SaveSketchAsync(GeneratedCode code, string folderPath);

    /// <summary>
    /// Validates that the generated code compiles (if Arduino CLI available)
    /// </summary>
    Task<CompilationResult> ValidateCompilationAsync(GeneratedCode code);
}

/// <summary>
/// Generated Arduino code with all files
/// </summary>
public class GeneratedCode
{
    /// <summary>
    /// Main .ino sketch file content
    /// </summary>
    public string MainSketch { get; set; } = string.Empty;

    /// <summary>
    /// Additional header files
    /// </summary>
    public Dictionary<string, string> HeaderFiles { get; set; } = [];

    /// <summary>
    /// Configuration data file (for EEPROM or PROGMEM)
    /// </summary>
    public string? ConfigData { get; set; }

    /// <summary>
    /// Sketch folder name
    /// </summary>
    public string SketchName { get; set; } = "ArduinoConfig";

    /// <summary>
    /// Required libraries
    /// </summary>
    public List<ArduinoLibrary> RequiredLibraries { get; set; } = [];
}

/// <summary>
/// Information about a required Arduino library
/// </summary>
public class ArduinoLibrary
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Url { get; set; }
    public string? IncludeStatement { get; set; }
}

/// <summary>
/// Result of compilation validation
/// </summary>
public class CompilationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public int? SketchSize { get; set; }
    public int? GlobalVariablesSize { get; set; }
}
