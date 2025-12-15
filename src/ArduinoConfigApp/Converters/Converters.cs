using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using ArduinoConfigApp.Core.Enums;

namespace ArduinoConfigApp.Converters;

/// <summary>
/// Converts bool to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// Converts bool to Visibility (true = Collapsed, false = Visible)
/// </summary>
public class BoolToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Collapsed;
    }
}

/// <summary>
/// Converts ConnectionState to a color for status indicators
/// </summary>
public class ConnectionStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var state = value is ConnectionState cs ? cs : ConnectionState.Disconnected;

        var color = state switch
        {
            ConnectionState.Connected => Colors.LimeGreen,
            ConnectionState.Connecting => Colors.Gold,
            ConnectionState.Error => Colors.OrangeRed,
            _ => Colors.Gray
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ConnectionState to a background brush for status panels
/// </summary>
public class ConnectionStateToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var state = value is ConnectionState cs ? cs : ConnectionState.Disconnected;

        var color = state switch
        {
            ConnectionState.Connected => Windows.UI.Color.FromArgb(40, 0, 200, 0),
            ConnectionState.Connecting => Windows.UI.Color.FromArgb(40, 255, 200, 0),
            ConnectionState.Error => Windows.UI.Color.FromArgb(40, 255, 100, 0),
            _ => Windows.UI.Color.FromArgb(40, 128, 128, 128)
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ConnectionState to button text (Connect/Disconnect)
/// </summary>
public class ConnectionButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var state = value is ConnectionState cs ? cs : ConnectionState.Disconnected;

        return state switch
        {
            ConnectionState.Connected => "Disconnect",
            ConnectionState.Connecting => "Connecting...",
            _ => "Connect"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to accent border brush (for active input indicators)
/// </summary>
public class BoolToAccentBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
        {
            return new SolidColorBrush(Colors.LimeGreen);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts InputType to Visibility (visible only for encoders)
/// </summary>
public class EncoderTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is InputType.RotaryEncoder ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Two-way converter for BoardType radio button binding
/// </summary>
public class BoardTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is BoardType boardType && parameter is string paramStr)
        {
            return Enum.TryParse<BoardType>(paramStr, out var expected) && boardType == expected;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is true && parameter is string paramStr)
        {
            if (Enum.TryParse<BoardType>(paramStr, out var result))
            {
                return result;
            }
        }
        return BoardType.ProMicro;
    }
}

/// <summary>
/// Converts InputType to an icon glyph
/// </summary>
public class InputTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            InputType.MomentaryButton => "\uE961",
            InputType.LatchingButton => "\uE961",
            InputType.RotaryEncoder => "\uE895",
            InputType.ToggleSwitch => "\uE7E8",
            _ => "\uE946"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a color string to SolidColorBrush
/// </summary>
public class StringToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string colorStr && colorStr.StartsWith("#"))
        {
            try
            {
                var color = Windows.UI.Color.FromArgb(
                    byte.Parse(colorStr.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(colorStr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(colorStr.Substring(5, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(colorStr.Length > 7 ? colorStr.Substring(7, 2) : "FF", System.Globalization.NumberStyles.HexNumber));
                return new SolidColorBrush(color);
            }
            catch
            {
                // Fall through to default
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
