using Microsoft.UI.Xaml.Controls;
using ArduinoConfigApp.ViewModels;

namespace ArduinoConfigApp.Views;

/// <summary>
/// Dashboard page showing connection status and project overview
/// </summary>
public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        // Get ViewModel from DI
        ViewModel = App.GetService<DashboardViewModel>();

        this.InitializeComponent();
    }
}
