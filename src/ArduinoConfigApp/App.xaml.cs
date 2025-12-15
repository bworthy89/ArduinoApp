using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Services.Serial;
using ArduinoConfigApp.Services.Configuration;
using ArduinoConfigApp.Services.CodeGeneration;
using ArduinoConfigApp.Services.InputTesting;
using ArduinoConfigApp.Services.WiringDiagram;
using ArduinoConfigApp.ViewModels;

namespace ArduinoConfigApp;

/// <summary>
/// Main application class with dependency injection setup
/// </summary>
public partial class App : Application
{
    private Window? _mainWindow;
    private static IServiceProvider? _services;

    public static IServiceProvider Services => _services ?? throw new InvalidOperationException("Services not initialized");

    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<ISerialService, SerialService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ICodeGenerationService, ArduinoCodeGenerator>();
        services.AddSingleton<IWiringDiagramService, WiringDiagramGenerator>();
        services.AddTransient<IInputTestingService, InputTestingService>();

        // Register view models
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<InputConfigViewModel>();
        services.AddTransient<InputTestingViewModel>();
        services.AddTransient<OutputMappingViewModel>();

        _services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
