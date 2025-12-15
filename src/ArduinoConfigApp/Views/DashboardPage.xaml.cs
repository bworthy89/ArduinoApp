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
        // In production, this would be injected via DI
        // ViewModel = App.GetService<DashboardViewModel>();

        this.InitializeComponent();
    }
}
