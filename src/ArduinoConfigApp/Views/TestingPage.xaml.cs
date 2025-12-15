using Microsoft.UI.Xaml.Controls;
using ArduinoConfigApp.ViewModels;

namespace ArduinoConfigApp.Views;

/// <summary>
/// Testing page for real-time input testing with visual feedback
/// </summary>
public sealed partial class TestingPage : Page
{
    public InputTestingViewModel ViewModel { get; }

    public TestingPage()
    {
        // Get ViewModel from DI
        ViewModel = App.GetService<InputTestingViewModel>();

        this.InitializeComponent();
    }
}
