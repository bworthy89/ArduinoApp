namespace ArduinoConfigApp.Services.Serial;

/// <summary>
/// Defines the serial communication protocol between the desktop app and Arduino
/// </summary>
public static class SerialProtocol
{
    /// <summary>
    /// Baud rate for serial communication
    /// </summary>
    public const int BaudRate = 115200;

    /// <summary>
    /// Command definitions sent from desktop to Arduino
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Ping command to verify connection
        /// Response: {"response":"PONG","success":true}
        /// </summary>
        public const string Ping = "PING";

        /// <summary>
        /// Get firmware version
        /// Response: {"response":"VERSION","success":true,"data":"1.0.0"}
        /// </summary>
        public const string Version = "VERSION";

        /// <summary>
        /// Get current state of all inputs
        /// Response: {"response":"STATE","success":true,"inputs":[...]}
        /// </summary>
        public const string GetState = "GET_STATE";

        /// <summary>
        /// Set display value
        /// Params: {"display":0,"value":12345,"brightness":8}
        /// </summary>
        public const string SetDisplay = "SET_DISPLAY";

        /// <summary>
        /// Test display with pattern
        /// Params: {"display":0,"pattern":"COUNT_UP"}
        /// </summary>
        public const string TestDisplay = "TEST_DISPLAY";

        /// <summary>
        /// Upload full configuration to Arduino
        /// Params: Full configuration JSON
        /// </summary>
        public const string UploadConfig = "UPLOAD_CONFIG";

        /// <summary>
        /// Save configuration to EEPROM
        /// </summary>
        public const string SaveConfig = "SAVE_CONFIG";

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public const string ResetConfig = "RESET_CONFIG";

        /// <summary>
        /// Enable/disable input testing mode
        /// Params: {"enabled":true}
        /// </summary>
        public const string TestMode = "TEST_MODE";

        /// <summary>
        /// Enable/disable keyboard output
        /// Params: {"enabled":true}
        /// </summary>
        public const string KeyboardMode = "KEYBOARD_MODE";
    }

    /// <summary>
    /// Response types from Arduino
    /// </summary>
    public static class Responses
    {
        public const string Pong = "PONG";
        public const string State = "STATE";
        public const string InputEvent = "INPUT_EVENT";
        public const string Error = "ERROR";
        public const string Ok = "OK";
    }

    /// <summary>
    /// Input event types from Arduino
    /// </summary>
    public static class InputEvents
    {
        public const string ButtonPressed = "BTN_PRESS";
        public const string ButtonReleased = "BTN_RELEASE";
        public const string EncoderCw = "ENC_CW";
        public const string EncoderCcw = "ENC_CCW";
        public const string EncoderPress = "ENC_PRESS";
        public const string ToggleOn = "TOG_ON";
        public const string ToggleOff = "TOG_OFF";
    }
}

/// <summary>
/// Example JSON protocol messages:
///
/// Desktop -> Arduino:
/// {"cmd":"PING"}
/// {"cmd":"GET_STATE"}
/// {"cmd":"SET_DISPLAY","params":{"display":0,"value":12345}}
/// {"cmd":"TEST_MODE","params":{"enabled":true}}
///
/// Arduino -> Desktop:
/// {"response":"PONG","success":true}
/// {"response":"STATE","success":true,"inputs":[{"id":0,"type":"BTN","state":1},{"id":1,"type":"ENC","value":100}]}
/// {"response":"INPUT_EVENT","type":"ENC_CW","id":1,"value":101}
/// {"response":"ERROR","message":"Invalid command"}
/// </summary>
public class ProtocolDocumentation { }
