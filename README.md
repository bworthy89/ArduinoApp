# Arduino Configuration Desktop App

A modern Windows desktop application for configuring and testing Arduino Pro Micro and Mega 2560 boards with customizable inputs, MAX7219-based 7-segment displays, keyboard output mappings, and real-time input testing.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Hardware Support**
  - Arduino Pro Micro (ATmega32U4) with native USB HID keyboard emulation
  - Arduino Mega 2560 (ATmega2560) with expanded I/O
  - KD2-22 Latching Buttons, Momentary Buttons
  - EC11 Rotary Encoders with push button
  - Toggle Switches
  - MAX7219-based 0.36" 8-digit 7-segment LED displays

- **Configuration**
  - Intuitive UI for adding and configuring inputs
  - Pin assignment with conflict detection
  - Map rotary encoders to display values (increments: 1, 10, 100, 1000)
  - Keyboard output mapping (single keys, combos, modifiers, special keys)
  - Save/load configurations as JSON

- **Real-time Testing**
  - Test buttons, encoders, and displays in real-time
  - Visual feedback panel with state indicators
  - Event logging with timestamps
  - Display preview with 7-segment rendering

- **Code Generation**
  - Automatically generate Arduino sketches from your configuration
  - Includes all necessary pin setup, input handling, and keyboard output code
  - SPI communication for MAX7219 displays

- **Wiring Assistance**
  - Generate SVG wiring diagrams based on your configuration
  - Step-by-step wiring guide with pin assignments
  - Parts list generation

## Screenshots

*Coming soon*

## Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 SDK
- Visual Studio 2022 (recommended) or VS Code

## Getting Started

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/bworthy89/ArduinoApp.git
   cd ArduinoApp
   ```

2. Restore dependencies:
   ```bash
   dotnet restore ArduinoConfigApp.sln
   ```

3. Build and run:
   ```bash
   dotnet build ArduinoConfigApp.sln
   dotnet run --project src/ArduinoConfigApp/ArduinoConfigApp.csproj
   ```

### Arduino Setup

1. Install the required Arduino libraries:
   - [ArduinoJson](https://github.com/bblanchon/ArduinoJson) (v6.x)
   - [LedControl](https://github.com/wayoda/LedControl)
   - [Encoder](https://github.com/PaulStoffregen/Encoder)

2. Configure your inputs and displays in the app

3. Generate the Arduino sketch (click "Generate Code")

4. Upload the generated sketch to your Arduino

## Project Structure

```
ArduinoConfigApp/
├── src/
│   ├── ArduinoConfigApp/              # WinUI 3 Application
│   │   ├── Views/                     # XAML pages
│   │   ├── ViewModels/                # MVVM ViewModels
│   │   ├── Converters/                # Value converters
│   │   └── Resources/                 # Styles and themes
│   │
│   ├── ArduinoConfigApp.Core/         # Domain layer
│   │   ├── Models/                    # Domain entities
│   │   ├── Interfaces/                # Service contracts
│   │   └── Enums/                     # Enumerations
│   │
│   ├── ArduinoConfigApp.Services/     # Application services
│   │   ├── Serial/                    # Arduino communication
│   │   ├── Configuration/             # JSON persistence
│   │   ├── CodeGeneration/            # Arduino sketch generator
│   │   ├── WiringDiagram/             # SVG diagram generator
│   │   └── InputTesting/              # Real-time testing
│   │
│   └── ArduinoConfigApp.Arduino/      # Arduino firmware templates
│
└── tests/                             # Unit and integration tests
```

## Architecture

The application follows **Clean Architecture** principles with the **MVVM** pattern:

- **Core Layer** - Domain models, interfaces, and enums (no external dependencies)
- **Services Layer** - Business logic and infrastructure services
- **Presentation Layer** - WinUI 3 views and view models

### Key Technologies

- [WinUI 3](https://docs.microsoft.com/windows/apps/winui/winui3/) - Modern Windows UI framework
- [CommunityToolkit.Mvvm](https://docs.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM framework
- [System.IO.Ports](https://docs.microsoft.com/dotnet/api/system.io.ports) - Serial communication

## Usage

### 1. Connect Your Arduino

1. Open the app and go to the **Dashboard**
2. Click **Scan** to detect available COM ports
3. Select your Arduino's port and click **Connect**

### 2. Configure Inputs

1. Navigate to the **Inputs** page
2. Click **Add Input** and select the input type
3. Assign pin numbers (the app prevents conflicts)
4. Configure debounce time and other options

### 3. Configure Displays

1. Navigate to the **Displays** page
2. Add a MAX7219 display and assign the CS pin
3. Link rotary encoders to control display values
4. Set brightness and display options

### 4. Map Keyboard Outputs

1. Navigate to the **Output Mapping** page
2. Select an input and add keyboard mappings
3. Choose the trigger action (press, release, rotate, etc.)
4. Select the key and optional modifiers (Ctrl, Shift, Alt, Win)

### 5. Test Your Configuration

1. Navigate to the **Testing** page
2. Click **Start Testing** to begin real-time monitoring
3. Press buttons, rotate encoders, and verify the visual feedback
4. Check the event log for detailed input events

### 6. Generate Code and Wiring

1. Click **Generate Code** to create the Arduino sketch
2. Navigate to **Wiring Guide** for the wiring diagram
3. Follow the step-by-step instructions to wire your components
4. Upload the generated sketch to your Arduino

## Serial Protocol

The app communicates with Arduino using a JSON-based protocol:

```json
// Commands (Desktop → Arduino)
{"cmd":"PING"}
{"cmd":"GET_STATE"}
{"cmd":"SET_DISPLAY","params":{"display":0,"value":12345}}

// Responses (Arduino → Desktop)
{"response":"PONG","success":true}
{"response":"STATE","success":true,"inputs":[{"id":0,"state":1}]}
{"response":"INPUT_EVENT","type":"ENC_CW","id":1,"value":101}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Arduino](https://www.arduino.cc/) - Open-source electronics platform
- [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml) - Modern Windows UI
- [ArduinoJson](https://arduinojson.org/) - JSON library for Arduino
- [LedControl](https://github.com/wayoda/LedControl) - MAX7219 library

---

Made with ❤️ for the Arduino community
