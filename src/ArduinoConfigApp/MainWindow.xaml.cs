using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ArduinoConfigApp.ViewModels;
using ArduinoConfigApp.Views;

namespace ArduinoConfigApp;

/// <summary>
/// Main application window with navigation
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        // Get ViewModel from DI
        ViewModel = App.GetService<MainViewModel>();

        // Set window size
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

        // Navigate to dashboard on startup
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            // Navigate to settings page
            // ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var selectedItem = args.SelectedItemContainer as NavigationViewItem;
        if (selectedItem == null)
            return;

        var tag = selectedItem.Tag?.ToString();

        var pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Inputs" => typeof(InputConfigPage),
            "Displays" => typeof(DisplayConfigPage),
            "OutputMapping" => typeof(OutputMappingPage),
            "Testing" => typeof(TestingPage),
            "Wiring" => typeof(WiringPage),
            "GenerateCode" => typeof(CodeGenerationPage),
            _ => typeof(DashboardPage)
        };

        ContentFrame.Navigate(pageType);
    }
}

// Placeholder page types - in a full implementation these would be in separate files
public partial class InputConfigPage : Page { }
public partial class DisplayConfigPage : Page { }
public partial class OutputMappingPage : Page { }
public partial class WiringPage : Page { }
public partial class CodeGenerationPage : Page { }
