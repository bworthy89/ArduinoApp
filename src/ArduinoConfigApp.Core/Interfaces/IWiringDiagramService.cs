using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Core.Interfaces;

/// <summary>
/// Service for generating wiring diagrams
/// </summary>
public interface IWiringDiagramService
{
    /// <summary>
    /// Generates a complete wiring diagram from the configuration
    /// </summary>
    WiringDiagram GenerateDiagram(ProjectConfiguration configuration);

    /// <summary>
    /// Generates step-by-step wiring instructions
    /// </summary>
    IReadOnlyList<WiringStep> GenerateWiringSteps(ProjectConfiguration configuration);

    /// <summary>
    /// Renders the diagram to an image
    /// </summary>
    Task<byte[]> RenderToImageAsync(WiringDiagram diagram, ImageFormat format, int width = 1200, int height = 800);

    /// <summary>
    /// Renders the diagram to SVG string
    /// </summary>
    string RenderToSvg(WiringDiagram diagram);

    /// <summary>
    /// Exports the diagram to a file
    /// </summary>
    Task ExportAsync(WiringDiagram diagram, string filePath, ImageFormat format);

    /// <summary>
    /// Gets a parts list for the configuration
    /// </summary>
    IReadOnlyList<PartInfo> GetPartsList(ProjectConfiguration configuration);
}

/// <summary>
/// Supported image export formats
/// </summary>
public enum ImageFormat
{
    Png,
    Svg,
    Pdf
}

/// <summary>
/// Information about a required part
/// </summary>
public class PartInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? PartNumber { get; set; }
    public string? PurchaseUrl { get; set; }
}
