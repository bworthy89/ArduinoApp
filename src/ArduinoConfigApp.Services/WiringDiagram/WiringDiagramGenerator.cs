using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.Services.WiringDiagram;

/// <summary>
/// Generates wiring diagrams and step-by-step wiring guides
/// Uses SVG rendering for scalable vector graphics output
/// </summary>
public class WiringDiagramGenerator : IWiringDiagramService
{
    // Component dimensions (in pixels at default scale)
    private const double ArduinoProMicroWidth = 200;
    private const double ArduinoProMicroHeight = 380;
    private const double ArduinoMegaWidth = 280;
    private const double ArduinoMegaHeight = 500;
    private const double ButtonWidth = 60;
    private const double ButtonHeight = 60;
    private const double EncoderWidth = 80;
    private const double EncoderHeight = 80;
    private const double DisplayWidth = 200;
    private const double DisplayHeight = 60;

    public WiringDiagram GenerateDiagram(ProjectConfiguration configuration)
    {
        var diagram = new WiringDiagram
        {
            Width = CalculateDiagramWidth(configuration),
            Height = CalculateDiagramHeight(configuration)
        };

        // Add Arduino board
        var arduinoComponent = CreateArduinoComponent(configuration.TargetBoard, diagram);
        diagram.Components.Add(arduinoComponent);

        // Add power rails
        var vccRail = CreatePowerRail(diagram, true);
        var gndRail = CreatePowerRail(diagram, false);
        diagram.Components.Add(vccRail);
        diagram.Components.Add(gndRail);

        // Add input components and their connections
        double componentX = 450;
        double componentY = 100;
        double componentSpacing = 120;

        foreach (var input in configuration.Inputs)
        {
            var component = CreateInputComponent(input, componentX, componentY);
            diagram.Components.Add(component);

            // Create wire connections
            var connections = CreateInputConnections(input, arduinoComponent, component, gndRail);
            diagram.Connections.AddRange(connections);

            componentY += componentSpacing;
        }

        // Add display components
        componentX = 650;
        componentY = 100;

        foreach (var display in configuration.Displays)
        {
            var component = CreateDisplayComponent(display, componentX, componentY);
            diagram.Components.Add(component);

            // Create SPI connections
            var connections = CreateDisplayConnections(display, configuration.TargetBoard, arduinoComponent, component, vccRail, gndRail);
            diagram.Connections.AddRange(connections);

            componentY += componentSpacing;
        }

        // Generate wiring steps
        diagram.WiringSteps = GenerateWiringSteps(configuration).ToList();

        return diagram;
    }

    public IReadOnlyList<WiringStep> GenerateWiringSteps(ProjectConfiguration configuration)
    {
        var steps = new List<WiringStep>();
        int stepNum = 1;

        // Step 1: Power connections
        steps.Add(new WiringStep
        {
            StepNumber = stepNum++,
            Title = "Connect Power Rails",
            Description = "Connect the VCC (5V/3.3V) and GND rails from the Arduino to your breadboard power rails.",
            Warning = "Ensure your power supply matches your components' voltage requirements (5V for most Arduino boards)."
        });

        // Step 2: Input components
        foreach (var input in configuration.Inputs)
        {
            var step = input switch
            {
                ButtonConfiguration btn => new WiringStep
                {
                    StepNumber = stepNum++,
                    Title = $"Wire Button: {input.Name}",
                    Description = $"Connect button '{input.Name}' to pin {btn.Pin}.\n" +
                        $"• One terminal to Arduino pin {btn.Pin}\n" +
                        $"• Other terminal to GND" +
                        (btn.UseInternalPullup ? "\n• Internal pull-up resistor will be enabled in firmware" :
                            "\n• Add a 10K pull-up resistor between the pin and VCC"),
                    Warning = btn.UseInternalPullup ? null : "Don't forget the pull-up resistor!"
                },

                EncoderConfiguration enc => new WiringStep
                {
                    StepNumber = stepNum++,
                    Title = $"Wire Encoder: {input.Name}",
                    Description = $"Connect EC11 rotary encoder '{input.Name}':\n" +
                        $"• CLK (A) to Arduino pin {enc.PinA}\n" +
                        $"• DT (B) to Arduino pin {enc.PinB}\n" +
                        (enc.ButtonPin >= 0 ? $"• SW (button) to Arduino pin {enc.ButtonPin}\n" : "") +
                        $"• + to VCC\n" +
                        $"• GND to GND",
                    Warning = "Ensure CLK and DT are connected to interrupt-capable pins for best performance."
                },

                ToggleSwitchConfiguration tog => new WiringStep
                {
                    StepNumber = stepNum++,
                    Title = $"Wire Toggle Switch: {input.Name}",
                    Description = $"Connect toggle switch '{input.Name}':\n" +
                        $"• Common terminal to Arduino pin {tog.Pin}\n" +
                        $"• NC/NO terminal to GND",
                    Warning = null
                },

                _ => new WiringStep
                {
                    StepNumber = stepNum++,
                    Title = $"Wire Input: {input.Name}",
                    Description = "Connect input according to manufacturer specifications."
                }
            };

            steps.Add(step);
        }

        // Step 3: Display connections
        if (configuration.Displays.Count > 0)
        {
            var board = new ArduinoBoard { BoardType = configuration.TargetBoard };

            steps.Add(new WiringStep
            {
                StepNumber = stepNum++,
                Title = "Wire SPI Bus (Common to All Displays)",
                Description = $"Connect SPI bus for MAX7219 displays:\n" +
                    $"• DIN (MOSI) to Arduino pin {board.SpiPins.Mosi}\n" +
                    $"• CLK (SCK) to Arduino pin {board.SpiPins.Sck}\n" +
                    $"• VCC to 5V\n" +
                    $"• GND to GND",
                Warning = "MAX7219 requires 5V power. Ensure your Arduino outputs 5V logic levels."
            });

            foreach (var display in configuration.Displays)
            {
                steps.Add(new WiringStep
                {
                    StepNumber = stepNum++,
                    Title = $"Wire Display CS: {display.Name}",
                    Description = $"Connect the CS (Chip Select) pin for display '{display.Name}':\n" +
                        $"• CS/LOAD to Arduino pin {display.CsPin}",
                    Warning = null
                });
            }
        }

        // Final verification step
        steps.Add(new WiringStep
        {
            StepNumber = stepNum++,
            Title = "Verify Connections",
            Description = "Before powering on:\n" +
                "• Double-check all connections match the diagram\n" +
                "• Ensure no loose wires or shorts\n" +
                "• Verify VCC is not connected to GND\n" +
                "• Check that all ground connections share a common ground",
            Warning = "Incorrect wiring can damage your Arduino or components. Verify carefully!"
        });

        return steps;
    }

    public async Task<byte[]> RenderToImageAsync(WiringDiagram diagram, ImageFormat format, int width = 1200, int height = 800)
    {
        // For PNG rendering, we would use SkiaSharp
        // This is a placeholder - actual implementation would render the SVG to bitmap
        var svg = RenderToSvg(diagram);

        if (format == ImageFormat.Svg)
        {
            return System.Text.Encoding.UTF8.GetBytes(svg);
        }

        // For PNG/PDF, you would use SkiaSharp or similar library
        // Placeholder: return SVG as bytes
        return await Task.FromResult(System.Text.Encoding.UTF8.GetBytes(svg));
    }

    public string RenderToSvg(WiringDiagram diagram)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        svg.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{diagram.Width}"" height=""{diagram.Height}"" viewBox=""0 0 {diagram.Width} {diagram.Height}"">");

        // Background
        svg.AppendLine($@"  <rect width=""100%"" height=""100%"" fill=""#f5f5f5""/>");

        // Styles
        svg.AppendLine(@"  <defs>
    <style>
      .component { fill: #fff; stroke: #333; stroke-width: 2; }
      .arduino { fill: #00979D; }
      .pin { fill: #333; }
      .pin-label { font-family: monospace; font-size: 10px; fill: #333; }
      .component-label { font-family: sans-serif; font-size: 12px; fill: #333; font-weight: bold; }
      .wire { fill: none; stroke-width: 2; stroke-linecap: round; }
      .wire-red { stroke: #e74c3c; }
      .wire-black { stroke: #333; }
      .wire-blue { stroke: #3498db; }
      .wire-green { stroke: #27ae60; }
      .wire-yellow { stroke: #f1c40f; }
      .wire-orange { stroke: #e67e22; }
      .wire-purple { stroke: #9b59b6; }
      .wire-white { stroke: #ecf0f1; stroke-width: 3; }
    </style>
  </defs>");

        // Draw wires first (behind components)
        foreach (var wire in diagram.Connections)
        {
            var fromComp = diagram.Components.FirstOrDefault(c => c.Id == wire.FromComponentId);
            var toComp = diagram.Components.FirstOrDefault(c => c.Id == wire.ToComponentId);

            if (fromComp == null || toComp == null) continue;

            var fromPin = fromComp.Pins.FirstOrDefault(p => p.Name == wire.FromPin);
            var toPin = toComp.Pins.FirstOrDefault(p => p.Name == wire.ToPin);

            if (fromPin == null || toPin == null) continue;

            var x1 = fromComp.X + fromPin.RelativeX;
            var y1 = fromComp.Y + fromPin.RelativeY;
            var x2 = toComp.X + toPin.RelativeX;
            var y2 = toComp.Y + toPin.RelativeY;

            var wireClass = $"wire wire-{wire.Color.ToString().ToLower()}";

            // Draw curved path for nicer wiring
            var midX = (x1 + x2) / 2;
            svg.AppendLine($@"  <path class=""{wireClass}"" d=""M{x1},{y1} C{midX},{y1} {midX},{y2} {x2},{y2}""/>");
        }

        // Draw components
        foreach (var component in diagram.Components)
        {
            svg.AppendLine(RenderComponent(component));
        }

        // Legend
        svg.AppendLine(RenderLegend(diagram));

        svg.AppendLine(@"</svg>");

        return svg.ToString();
    }

    private string RenderComponent(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"  <g transform=""translate({component.X}, {component.Y})"">");

        switch (component.Type)
        {
            case ComponentType.ArduinoProMicro:
            case ComponentType.ArduinoMega2560:
                svg.AppendLine(RenderArduino(component));
                break;

            case ComponentType.MomentaryButton:
            case ComponentType.LatchingButton:
                svg.AppendLine(RenderButton(component));
                break;

            case ComponentType.RotaryEncoder:
                svg.AppendLine(RenderEncoder(component));
                break;

            case ComponentType.ToggleSwitch:
                svg.AppendLine(RenderToggleSwitch(component));
                break;

            case ComponentType.Max7219Display:
                svg.AppendLine(RenderDisplay(component));
                break;

            case ComponentType.PowerRail:
            case ComponentType.GroundRail:
                svg.AppendLine(RenderPowerRail(component));
                break;
        }

        svg.AppendLine(@"  </g>");

        return svg.ToString();
    }

    private string RenderArduino(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();
        var isProMicro = component.Type == ComponentType.ArduinoProMicro;

        svg.AppendLine($@"    <rect class=""component arduino"" width=""{component.Width}"" height=""{component.Height}"" rx=""5""/>");
        svg.AppendLine($@"    <text class=""component-label"" x=""{component.Width / 2}"" y=""25"" text-anchor=""middle"" fill=""white"">{component.Name}</text>");

        // Draw pins
        foreach (var pin in component.Pins)
        {
            svg.AppendLine($@"    <circle class=""pin"" cx=""{pin.RelativeX}"" cy=""{pin.RelativeY}"" r=""4""/>");
            var labelX = pin.Side == PinSide.Left ? pin.RelativeX - 8 : pin.RelativeX + 8;
            var anchor = pin.Side == PinSide.Left ? "end" : "start";
            svg.AppendLine($@"    <text class=""pin-label"" x=""{labelX}"" y=""{pin.RelativeY + 4}"" text-anchor=""{anchor}"">{pin.Name}</text>");
        }

        return svg.ToString();
    }

    private string RenderButton(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"    <rect class=""component"" width=""{component.Width}"" height=""{component.Height}"" rx=""5""/>");
        svg.AppendLine($@"    <circle cx=""{component.Width / 2}"" cy=""{component.Height / 2}"" r=""15"" fill=""#e74c3c""/>");
        svg.AppendLine($@"    <text class=""component-label"" x=""{component.Width / 2}"" y=""{component.Height + 15}"" text-anchor=""middle"">{component.Name}</text>");

        foreach (var pin in component.Pins)
        {
            svg.AppendLine($@"    <circle class=""pin"" cx=""{pin.RelativeX}"" cy=""{pin.RelativeY}"" r=""3""/>");
        }

        return svg.ToString();
    }

    private string RenderEncoder(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"    <rect class=""component"" width=""{component.Width}"" height=""{component.Height}"" rx=""5""/>");
        svg.AppendLine($@"    <circle cx=""{component.Width / 2}"" cy=""{component.Height / 2}"" r=""20"" fill=""#333""/>");
        svg.AppendLine($@"    <circle cx=""{component.Width / 2}"" cy=""{component.Height / 2}"" r=""8"" fill=""#666""/>");
        svg.AppendLine($@"    <text class=""component-label"" x=""{component.Width / 2}"" y=""{component.Height + 15}"" text-anchor=""middle"">{component.Name}</text>");

        foreach (var pin in component.Pins)
        {
            svg.AppendLine($@"    <circle class=""pin"" cx=""{pin.RelativeX}"" cy=""{pin.RelativeY}"" r=""3""/>");
            svg.AppendLine($@"    <text class=""pin-label"" x=""{pin.RelativeX}"" y=""{pin.RelativeY + 15}"" text-anchor=""middle"" font-size=""8"">{pin.Name}</text>");
        }

        return svg.ToString();
    }

    private string RenderToggleSwitch(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"    <rect class=""component"" width=""{component.Width}"" height=""{component.Height}"" rx=""3""/>");
        svg.AppendLine($@"    <rect x=""10"" y=""15"" width=""40"" height=""30"" fill=""#333"" rx=""2""/>");
        svg.AppendLine($@"    <rect x=""15"" y=""20"" width=""15"" height=""20"" fill=""#666"" rx=""2""/>");
        svg.AppendLine($@"    <text class=""component-label"" x=""{component.Width / 2}"" y=""{component.Height + 15}"" text-anchor=""middle"">{component.Name}</text>");

        foreach (var pin in component.Pins)
        {
            svg.AppendLine($@"    <circle class=""pin"" cx=""{pin.RelativeX}"" cy=""{pin.RelativeY}"" r=""3""/>");
        }

        return svg.ToString();
    }

    private string RenderDisplay(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();

        svg.AppendLine($@"    <rect class=""component"" width=""{component.Width}"" height=""{component.Height}"" rx=""3""/>");
        svg.AppendLine($@"    <rect x=""10"" y=""10"" width=""{component.Width - 20}"" height=""{component.Height - 20}"" fill=""#111""/>");

        // Draw 7-segment digits
        for (int i = 0; i < 8; i++)
        {
            var digitX = 15 + i * 22;
            svg.AppendLine($@"    <text x=""{digitX}"" y=""42"" fill=""#f00"" font-family=""monospace"" font-size=""24"">8</text>");
        }

        svg.AppendLine($@"    <text class=""component-label"" x=""{component.Width / 2}"" y=""{component.Height + 15}"" text-anchor=""middle"">{component.Name}</text>");

        foreach (var pin in component.Pins)
        {
            svg.AppendLine($@"    <circle class=""pin"" cx=""{pin.RelativeX}"" cy=""{pin.RelativeY}"" r=""3""/>");
            svg.AppendLine($@"    <text class=""pin-label"" x=""{pin.RelativeX}"" y=""{pin.RelativeY - 8}"" text-anchor=""middle"" font-size=""8"">{pin.Name}</text>");
        }

        return svg.ToString();
    }

    private string RenderPowerRail(DiagramComponent component)
    {
        var svg = new System.Text.StringBuilder();
        var isVcc = component.Type == ComponentType.PowerRail;

        svg.AppendLine($@"    <rect width=""{component.Width}"" height=""{component.Height}"" fill=""{(isVcc ? "#e74c3c" : "#333")}"" rx=""2""/>");
        svg.AppendLine($@"    <text x=""{component.Width / 2}"" y=""{component.Height / 2 + 4}"" text-anchor=""middle"" fill=""white"" font-size=""10"">{component.Name}</text>");

        return svg.ToString();
    }

    private string RenderLegend(WiringDiagram diagram)
    {
        var svg = new System.Text.StringBuilder();
        var legendY = diagram.Height - 80;

        svg.AppendLine($@"  <g transform=""translate(20, {legendY})"">");
        svg.AppendLine(@"    <text font-weight=""bold"" y=""15"">Wire Colors:</text>");
        svg.AppendLine(@"    <line x1=""100"" y1=""10"" x2=""130"" y2=""10"" class=""wire wire-red""/><text x=""135"" y=""15"">VCC (5V)</text>");
        svg.AppendLine(@"    <line x1=""200"" y1=""10"" x2=""230"" y2=""10"" class=""wire wire-black""/><text x=""235"" y=""15"">GND</text>");
        svg.AppendLine(@"    <line x1=""280"" y1=""10"" x2=""310"" y2=""10"" class=""wire wire-blue""/><text x=""315"" y=""15"">Signal</text>");
        svg.AppendLine(@"    <line x1=""370"" y1=""10"" x2=""400"" y2=""10"" class=""wire wire-orange""/><text x=""405"" y=""15"">SPI CLK</text>");
        svg.AppendLine(@"    <line x1=""470"" y1=""10"" x2=""500"" y2=""10"" class=""wire wire-purple""/><text x=""505"" y=""15"">SPI Data</text>");
        svg.AppendLine(@"  </g>");

        return svg.ToString();
    }

    public async Task ExportAsync(WiringDiagram diagram, string filePath, ImageFormat format)
    {
        var data = await RenderToImageAsync(diagram, format, diagram.Width, diagram.Height);
        await File.WriteAllBytesAsync(filePath, data);
    }

    public IReadOnlyList<PartInfo> GetPartsList(ProjectConfiguration configuration)
    {
        var parts = new List<PartInfo>
        {
            new()
            {
                Name = configuration.TargetBoard == BoardType.ProMicro ? "Arduino Pro Micro" : "Arduino Mega 2560",
                Description = configuration.TargetBoard == BoardType.ProMicro
                    ? "ATmega32U4 microcontroller with native USB HID support"
                    : "ATmega2560 microcontroller with 54 digital I/O pins",
                Quantity = 1
            }
        };

        // Count buttons
        var buttonCount = configuration.Inputs.Count(i => i is ButtonConfiguration);
        if (buttonCount > 0)
        {
            var latchingCount = configuration.Inputs.Count(i => i is ButtonConfiguration { IsLatching: true });
            var momentaryCount = buttonCount - latchingCount;

            if (momentaryCount > 0)
            {
                parts.Add(new PartInfo
                {
                    Name = "Momentary Push Button",
                    Description = "Tactile push button (normally open)",
                    Quantity = momentaryCount
                });
            }

            if (latchingCount > 0)
            {
                parts.Add(new PartInfo
                {
                    Name = "KD2-22 Latching Button",
                    Description = "Push-on/push-off latching button",
                    Quantity = latchingCount
                });
            }
        }

        // Count encoders
        var encoderCount = configuration.Inputs.Count(i => i is EncoderConfiguration);
        if (encoderCount > 0)
        {
            parts.Add(new PartInfo
            {
                Name = "EC11 Rotary Encoder",
                Description = "Incremental rotary encoder with push button",
                Quantity = encoderCount
            });
        }

        // Count toggle switches
        var toggleCount = configuration.Inputs.Count(i => i is ToggleSwitchConfiguration);
        if (toggleCount > 0)
        {
            parts.Add(new PartInfo
            {
                Name = "Toggle Switch",
                Description = "SPST or SPDT toggle switch",
                Quantity = toggleCount
            });
        }

        // Count displays
        if (configuration.Displays.Count > 0)
        {
            parts.Add(new PartInfo
            {
                Name = "MAX7219 8-Digit 7-Segment Display",
                Description = "0.36\" LED display module with MAX7219 driver",
                Quantity = configuration.Displays.Count
            });
        }

        // Common parts
        parts.Add(new PartInfo
        {
            Name = "Breadboard",
            Description = "830-point solderless breadboard (or larger)",
            Quantity = 1
        });

        parts.Add(new PartInfo
        {
            Name = "Jumper Wires",
            Description = "Male-to-male jumper wire kit",
            Quantity = 1
        });

        return parts;
    }

    // Helper methods for creating diagram components
    private DiagramComponent CreateArduinoComponent(BoardType boardType, WiringDiagram diagram)
    {
        var isProMicro = boardType == BoardType.ProMicro;
        var component = new DiagramComponent
        {
            Name = isProMicro ? "Arduino Pro Micro" : "Arduino Mega 2560",
            Type = isProMicro ? ComponentType.ArduinoProMicro : ComponentType.ArduinoMega2560,
            X = 50,
            Y = 50,
            Width = isProMicro ? ArduinoProMicroWidth : ArduinoMegaWidth,
            Height = isProMicro ? ArduinoProMicroHeight : ArduinoMegaHeight
        };

        // Add pins based on board type
        var board = new ArduinoBoard { BoardType = boardType };
        var pinY = 50;
        var pinSpacing = 20;

        foreach (var pin in board.AvailablePins.Take(20)) // Limit for diagram clarity
        {
            component.Pins.Add(new DiagramPin
            {
                Name = pin.Label,
                PinNumber = pin.PinNumber,
                RelativeX = component.Width,
                RelativeY = pinY,
                Side = PinSide.Right
            });
            pinY += pinSpacing;
        }

        // Add power pins
        component.Pins.Add(new DiagramPin { Name = "VCC", PinNumber = -1, RelativeX = 0, RelativeY = 50, Side = PinSide.Left });
        component.Pins.Add(new DiagramPin { Name = "GND", PinNumber = -2, RelativeX = 0, RelativeY = 70, Side = PinSide.Left });

        return component;
    }

    private DiagramComponent CreatePowerRail(WiringDiagram diagram, bool isVcc)
    {
        return new DiagramComponent
        {
            Name = isVcc ? "VCC (5V)" : "GND",
            Type = isVcc ? ComponentType.PowerRail : ComponentType.GroundRail,
            X = 50,
            Y = isVcc ? 20 : diagram.Height - 40,
            Width = 300,
            Height = 15,
            Pins =
            [
                new DiagramPin { Name = isVcc ? "VCC" : "GND", RelativeX = 150, RelativeY = 7, Side = PinSide.Bottom }
            ]
        };
    }

    private DiagramComponent CreateInputComponent(InputConfiguration input, double x, double y)
    {
        return input switch
        {
            ButtonConfiguration btn => new DiagramComponent
            {
                Name = input.Name,
                Type = btn.IsLatching ? ComponentType.LatchingButton : ComponentType.MomentaryButton,
                X = x,
                Y = y,
                Width = ButtonWidth,
                Height = ButtonHeight,
                Pins =
                [
                    new DiagramPin { Name = "SIG", PinNumber = btn.Pin, RelativeX = 0, RelativeY = 30, Side = PinSide.Left },
                    new DiagramPin { Name = "GND", PinNumber = -2, RelativeX = ButtonWidth, RelativeY = 30, Side = PinSide.Right }
                ]
            },

            EncoderConfiguration enc => new DiagramComponent
            {
                Name = input.Name,
                Type = ComponentType.RotaryEncoder,
                X = x,
                Y = y,
                Width = EncoderWidth,
                Height = EncoderHeight,
                Pins =
                [
                    new DiagramPin { Name = "CLK", PinNumber = enc.PinA, RelativeX = 10, RelativeY = EncoderHeight, Side = PinSide.Bottom },
                    new DiagramPin { Name = "DT", PinNumber = enc.PinB, RelativeX = 30, RelativeY = EncoderHeight, Side = PinSide.Bottom },
                    new DiagramPin { Name = "SW", PinNumber = enc.ButtonPin, RelativeX = 50, RelativeY = EncoderHeight, Side = PinSide.Bottom },
                    new DiagramPin { Name = "VCC", PinNumber = -1, RelativeX = 65, RelativeY = EncoderHeight, Side = PinSide.Bottom },
                    new DiagramPin { Name = "GND", PinNumber = -2, RelativeX = 75, RelativeY = EncoderHeight, Side = PinSide.Bottom }
                ]
            },

            ToggleSwitchConfiguration tog => new DiagramComponent
            {
                Name = input.Name,
                Type = ComponentType.ToggleSwitch,
                X = x,
                Y = y,
                Width = ButtonWidth,
                Height = ButtonHeight,
                Pins =
                [
                    new DiagramPin { Name = "SIG", PinNumber = tog.Pin, RelativeX = 0, RelativeY = 30, Side = PinSide.Left },
                    new DiagramPin { Name = "GND", PinNumber = -2, RelativeX = ButtonWidth, RelativeY = 30, Side = PinSide.Right }
                ]
            },

            _ => new DiagramComponent { Name = input.Name, X = x, Y = y }
        };
    }

    private DiagramComponent CreateDisplayComponent(DisplayConfiguration display, double x, double y)
    {
        return new DiagramComponent
        {
            Name = display.Name,
            Type = ComponentType.Max7219Display,
            X = x,
            Y = y,
            Width = DisplayWidth,
            Height = DisplayHeight,
            Pins =
            [
                new DiagramPin { Name = "VCC", PinNumber = -1, RelativeX = 20, RelativeY = 0, Side = PinSide.Top },
                new DiagramPin { Name = "GND", PinNumber = -2, RelativeX = 50, RelativeY = 0, Side = PinSide.Top },
                new DiagramPin { Name = "DIN", PinNumber = -3, RelativeX = 80, RelativeY = 0, Side = PinSide.Top },
                new DiagramPin { Name = "CS", PinNumber = display.CsPin, RelativeX = 110, RelativeY = 0, Side = PinSide.Top },
                new DiagramPin { Name = "CLK", PinNumber = -4, RelativeX = 140, RelativeY = 0, Side = PinSide.Top }
            ]
        };
    }

    private List<WireConnection> CreateInputConnections(InputConfiguration input, DiagramComponent arduino, DiagramComponent component, DiagramComponent gndRail)
    {
        var connections = new List<WireConnection>();

        // Signal wire to Arduino
        foreach (var pin in input.UsedPins)
        {
            var arduinoPin = arduino.Pins.FirstOrDefault(p => p.PinNumber == pin);
            var componentPin = component.Pins.FirstOrDefault(p => p.PinNumber == pin);

            if (arduinoPin != null && componentPin != null)
            {
                connections.Add(new WireConnection
                {
                    FromComponentId = arduino.Id,
                    FromPin = arduinoPin.Name,
                    ToComponentId = component.Id,
                    ToPin = componentPin.Name,
                    Color = WireColor.Blue
                });
            }
        }

        // Ground wire
        var gndPin = component.Pins.FirstOrDefault(p => p.Name == "GND");
        if (gndPin != null)
        {
            connections.Add(new WireConnection
            {
                FromComponentId = component.Id,
                FromPin = "GND",
                ToComponentId = gndRail.Id,
                ToPin = "GND",
                Color = WireColor.Black
            });
        }

        return connections;
    }

    private List<WireConnection> CreateDisplayConnections(DisplayConfiguration display, BoardType boardType, DiagramComponent arduino, DiagramComponent component, DiagramComponent vccRail, DiagramComponent gndRail)
    {
        var connections = new List<WireConnection>();
        var board = new ArduinoBoard { BoardType = boardType };

        // VCC
        connections.Add(new WireConnection
        {
            FromComponentId = vccRail.Id,
            FromPin = "VCC",
            ToComponentId = component.Id,
            ToPin = "VCC",
            Color = WireColor.Red
        });

        // GND
        connections.Add(new WireConnection
        {
            FromComponentId = gndRail.Id,
            FromPin = "GND",
            ToComponentId = component.Id,
            ToPin = "GND",
            Color = WireColor.Black
        });

        // DIN (MOSI)
        var mosiPin = arduino.Pins.FirstOrDefault(p => p.PinNumber == board.SpiPins.Mosi);
        if (mosiPin != null)
        {
            connections.Add(new WireConnection
            {
                FromComponentId = arduino.Id,
                FromPin = mosiPin.Name,
                ToComponentId = component.Id,
                ToPin = "DIN",
                Color = WireColor.Purple
            });
        }

        // CLK (SCK)
        var sckPin = arduino.Pins.FirstOrDefault(p => p.PinNumber == board.SpiPins.Sck);
        if (sckPin != null)
        {
            connections.Add(new WireConnection
            {
                FromComponentId = arduino.Id,
                FromPin = sckPin.Name,
                ToComponentId = component.Id,
                ToPin = "CLK",
                Color = WireColor.Orange
            });
        }

        // CS pin
        var csPin = arduino.Pins.FirstOrDefault(p => p.PinNumber == display.CsPin);
        if (csPin != null)
        {
            connections.Add(new WireConnection
            {
                FromComponentId = arduino.Id,
                FromPin = csPin.Name,
                ToComponentId = component.Id,
                ToPin = "CS",
                Color = WireColor.Green
            });
        }

        return connections;
    }

    private int CalculateDiagramWidth(ProjectConfiguration config)
    {
        return Math.Max(900, 450 + config.Displays.Count * 250);
    }

    private int CalculateDiagramHeight(ProjectConfiguration config)
    {
        var inputHeight = config.Inputs.Count * 120 + 200;
        var displayHeight = config.Displays.Count * 100 + 200;
        return Math.Max(600, Math.Max(inputHeight, displayHeight));
    }
}
