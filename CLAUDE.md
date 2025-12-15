# CLAUDE.md - Arduino Configuration Desktop App

This document provides guidance for Claude Code when working with this codebase.

## Project Overview

A Windows desktop application for configuring and testing Arduino Pro Micro and Mega 2560 boards with customizable inputs, MAX7219-based 7-segment displays, keyboard output mappings, and real-time input testing.

**Tech Stack:**
- **Platform:** Windows Desktop (WinUI 3)
- **Language:** C# (.NET 8)
- **UI Framework:** WinUI 3 with MVVM pattern
- **Architecture:** Clean Architecture (Core → Services → Presentation)

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore ArduinoConfigApp.sln

# Build the solution
dotnet build ArduinoConfigApp.sln

# Build release
dotnet build ArduinoConfigApp.sln -c Release

# Run the application
dotnet run --project src/ArduinoConfigApp/ArduinoConfigApp.csproj

# Run tests
dotnet test tests/ArduinoConfigApp.Core.Tests/ArduinoConfigApp.Core.Tests.csproj
dotnet test tests/ArduinoConfigApp.Services.Tests/ArduinoConfigApp.Services.Tests.csproj
```

## Project Structure

```
ArduinoConfigApp/
├── src/
│   ├── ArduinoConfigApp/              # WinUI 3 Application (Presentation Layer)
│   │   ├── Views/                     # XAML pages
│   │   ├── ViewModels/                # MVVM ViewModels
│   │   ├── Converters/                # XAML value converters
│   │   ├── Controls/                  # Custom controls
│   │   └── Resources/                 # Styles, themes, assets
│   │
│   ├── ArduinoConfigApp.Core/         # Domain Layer (no dependencies)
│   │   ├── Models/                    # Domain entities
│   │   ├── Interfaces/                # Service contracts
│   │   └── Enums/                     # Enumerations
│   │
│   ├── ArduinoConfigApp.Services/     # Application Layer
│   │   ├── Serial/                    # Arduino serial communication
│   │   ├── Configuration/             # JSON config persistence
│   │   ├── CodeGeneration/            # Arduino sketch generator
│   │   ├── WiringDiagram/             # SVG diagram generator
│   │   └── InputTesting/              # Real-time input testing
│   │
│   └── ArduinoConfigApp.Arduino/      # Arduino firmware templates
│       ├── ProMicro/
│       └── Mega2560/
│
├── tests/
│   ├── ArduinoConfigApp.Core.Tests/
│   └── ArduinoConfigApp.Services.Tests/
│
├── docs/                              # Documentation
└── installer/                         # Windows installer files
```

## Key Architectural Patterns

### MVVM Pattern
- **Views** (XAML) bind to **ViewModels** via `{x:Bind}`
- ViewModels use `CommunityToolkit.Mvvm` for `[ObservableProperty]` and `[RelayCommand]`
- Services are injected via constructor DI

### Dependency Injection
Services are registered in `App.xaml.cs`:
```csharp
services.AddSingleton<ISerialService, SerialService>();
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<ICodeGenerationService, ArduinoCodeGenerator>();
services.AddSingleton<IWiringDiagramService, WiringDiagramGenerator>();
services.AddTransient<IInputTestingService, InputTestingService>();
```

### Serial Protocol
JSON-based command/response protocol between desktop and Arduino:
```json
// Desktop → Arduino
{"cmd":"PING"}
{"cmd":"GET_STATE"}
{"cmd":"SET_DISPLAY","params":{"display":0,"value":12345}}

// Arduino → Desktop
{"response":"PONG","success":true}
{"response":"STATE","success":true,"inputs":[{"id":0,"state":1}]}
{"response":"INPUT_EVENT","type":"ENC_CW","id":1,"value":101}
```

## Key Files Reference

| Purpose | File |
|---------|------|
| Main window/navigation | `src/ArduinoConfigApp/MainWindow.xaml(.cs)` |
| DI setup | `src/ArduinoConfigApp/App.xaml.cs` |
| Serial communication | `src/ArduinoConfigApp.Services/Serial/SerialService.cs` |
| Serial protocol | `src/ArduinoConfigApp.Services/Serial/SerialProtocol.cs` |
| Configuration CRUD | `src/ArduinoConfigApp.Services/Configuration/ConfigurationService.cs` |
| Arduino code gen | `src/ArduinoConfigApp.Services/CodeGeneration/ArduinoCodeGenerator.cs` |
| Wiring diagrams | `src/ArduinoConfigApp.Services/WiringDiagram/WiringDiagramGenerator.cs` |
| Input testing | `src/ArduinoConfigApp.Services/InputTesting/InputTestingService.cs` |
| Domain models | `src/ArduinoConfigApp.Core/Models/` |
| Arduino firmware | `src/ArduinoConfigApp.Arduino/ProMicro/ArduinoConfigFirmware/` |

## Domain Models

### Input Types
- `ButtonConfiguration` - Momentary or latching buttons (KD2-22)
- `EncoderConfiguration` - EC11 rotary encoders with optional button
- `ToggleSwitchConfiguration` - ON/OFF toggle switches

### Other Models
- `DisplayConfiguration` - MAX7219 7-segment display settings
- `KeyboardMapping` - Maps inputs to keyboard outputs
- `ProjectConfiguration` - Root configuration object (saved as JSON)
- `WiringDiagram` - Generated wiring diagram with components and connections

## Supported Hardware

### Arduino Boards
- **Pro Micro** (ATmega32U4) - Native USB HID for keyboard emulation
- **Mega 2560** (ATmega2560) - More pins, no native HID

### Input Components
- KD2-22 Latching Button
- Momentary Push Buttons
- EC11 Rotary Encoders
- Toggle Switches

### Display Components
- MAX7219-based 0.36" 8-bit 7-segment LED modules (SPI)

## Common Development Tasks

### Adding a New Input Type
1. Add enum value to `Core/Enums/InputType.cs`
2. Create configuration class in `Core/Models/InputConfiguration.cs`
3. Update `ArduinoCodeGenerator.cs` for code generation
4. Update `WiringDiagramGenerator.cs` for diagram support
5. Add UI in `InputConfigViewModel.cs` and corresponding XAML

### Adding a New Serial Command
1. Add command constant to `Serial/SerialProtocol.cs`
2. Implement handler in Arduino firmware
3. Add method to `ISerialService` interface
4. Implement in `SerialService.cs`

### Adding a New Page/View
1. Create `Views/NewPage.xaml` and code-behind
2. Create `ViewModels/NewPageViewModel.cs`
3. Register ViewModel in `App.xaml.cs` DI container
4. Add navigation item in `MainWindow.xaml`
5. Add case in `MainWindow.xaml.cs` navigation handler

## Testing Guidelines

- Unit test services in isolation with mocked dependencies
- Test configuration serialization/deserialization round-trips
- Test pin conflict detection in `ConfigurationService`
- Test Arduino code generation produces valid syntax
- Integration test serial communication with mock serial port

## Code Style

- Use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`)
- Prefer `async/await` for all I/O operations
- Use `IReadOnlyList<T>` for public collection properties
- Document public APIs with XML comments
- Keep ViewModels thin - business logic goes in Services

## Arduino Libraries Required

When uploading firmware to Arduino:
```
ArduinoJson (v6.x)    - JSON serial protocol
LedControl            - MAX7219 display control
Encoder               - Rotary encoder handling
Keyboard              - HID keyboard (Pro Micro only)
```

## Known Limitations

- MAX7219 SPI is controlled by Arduino, not directly by desktop app
- Mega 2560 cannot act as USB HID keyboard (no native USB)
- Wiring diagram export currently supports SVG only (PNG requires SkiaSharp)
- Arduino CLI integration for compilation validation not yet implemented
