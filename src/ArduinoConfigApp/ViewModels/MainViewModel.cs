using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;
using ArduinoConfigApp.Core.Models;

namespace ArduinoConfigApp.ViewModels;

/// <summary>
/// Main view model coordinating the application state and navigation
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ISerialService _serialService;
    private readonly IConfigurationService _configService;
    private readonly IInputTestingService _testingService;
    private readonly ICodeGenerationService _codeGenService;
    private readonly IWiringDiagramService _wiringService;

    [ObservableProperty]
    private string _currentPage = "Dashboard";

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private string? _connectedPort;

    [ObservableProperty]
    private string _projectName = "New Project";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isDarkTheme;

    public MainViewModel(
        ISerialService serialService,
        IConfigurationService configService,
        IInputTestingService testingService,
        ICodeGenerationService codeGenService,
        IWiringDiagramService wiringService)
    {
        _serialService = serialService;
        _configService = configService;
        _testingService = testingService;
        _codeGenService = codeGenService;
        _wiringService = wiringService;

        // Subscribe to events
        _serialService.ConnectionStateChanged += OnConnectionStateChanged;
        _configService.ConfigurationChanged += OnConfigurationChanged;
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        if (HasUnsavedChanges)
        {
            // TODO: Show save confirmation dialog
        }

        _configService.CreateNew("New Project");
        ProjectName = "New Project";
        StatusMessage = "Created new project";
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        // TODO: Show file picker dialog
        // For now, placeholder implementation
        StatusMessage = "Opening project...";
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (string.IsNullOrEmpty(_configService.CurrentFilePath))
        {
            await SaveProjectAsAsync();
            return;
        }

        await _configService.SaveAsync();
        StatusMessage = "Project saved";
    }

    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        // TODO: Show save file dialog
        StatusMessage = "Saving project...";
    }

    [RelayCommand]
    private async Task GenerateCodeAsync()
    {
        if (_configService.CurrentConfiguration == null)
        {
            StatusMessage = "No configuration loaded";
            return;
        }

        var validation = _configService.Validate();
        if (!validation.IsValid)
        {
            StatusMessage = $"Configuration has errors: {validation.Errors.FirstOrDefault()}";
            return;
        }

        var code = _codeGenService.GenerateSketch(_configService.CurrentConfiguration);

        // TODO: Show save folder dialog
        StatusMessage = $"Generated Arduino sketch: {code.SketchName}";
    }

    [RelayCommand]
    private async Task ExportWiringDiagramAsync()
    {
        if (_configService.CurrentConfiguration == null)
        {
            StatusMessage = "No configuration loaded";
            return;
        }

        var diagram = _wiringService.GenerateDiagram(_configService.CurrentConfiguration);

        // TODO: Show save file dialog
        StatusMessage = "Wiring diagram exported";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // TODO: Apply theme to application
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        ConnectionState = e.NewState;
        ConnectedPort = e.PortName;

        StatusMessage = e.NewState switch
        {
            ConnectionState.Connected => $"Connected to {e.PortName}",
            ConnectionState.Connecting => "Connecting...",
            ConnectionState.Disconnected => "Disconnected",
            ConnectionState.Error => "Connection error",
            _ => StatusMessage
        };
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        HasUnsavedChanges = _configService.HasUnsavedChanges;

        if (_configService.CurrentConfiguration != null)
        {
            ProjectName = _configService.CurrentConfiguration.Name;
        }
    }
}
