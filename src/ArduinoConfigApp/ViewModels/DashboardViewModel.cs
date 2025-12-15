using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArduinoConfigApp.Core.Enums;
using ArduinoConfigApp.Core.Interfaces;

namespace ArduinoConfigApp.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page showing connected boards and project status
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly ISerialService _serialService;
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private string? _firmwareVersion;

    [ObservableProperty]
    private BoardType _selectedBoardType = BoardType.ProMicro;

    [ObservableProperty]
    private int _inputCount;

    [ObservableProperty]
    private int _displayCount;

    [ObservableProperty]
    private int _mappingCount;

    [ObservableProperty]
    private bool _isScanning;

    public ObservableCollection<PortInfo> AvailablePorts { get; } = [];

    public DashboardViewModel(ISerialService serialService, IConfigurationService configService)
    {
        _serialService = serialService;
        _configService = configService;

        _serialService.ConnectionStateChanged += OnConnectionStateChanged;
        _configService.ConfigurationChanged += OnConfigurationChanged;

        UpdateConfigurationStats();
    }

    [RelayCommand]
    private async Task ScanPortsAsync()
    {
        IsScanning = true;
        AvailablePorts.Clear();

        try
        {
            var ports = await _serialService.GetAvailablePortsAsync();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }

            // Auto-select first Arduino port if found
            var arduinoPort = ports.FirstOrDefault(p => p.IsArduino);
            if (arduinoPort != null)
            {
                SelectedPort = arduinoPort.PortName;
            }
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(SelectedPort))
            return;

        if (ConnectionState == ConnectionState.Connected)
        {
            await _serialService.DisconnectAsync();
        }
        else
        {
            var success = await _serialService.ConnectAsync(SelectedPort);
            if (success)
            {
                FirmwareVersion = await _serialService.GetFirmwareVersionAsync();
            }
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _serialService.DisconnectAsync();
        FirmwareVersion = null;
    }

    [RelayCommand]
    private void SetBoardType(BoardType boardType)
    {
        SelectedBoardType = boardType;

        if (_configService.CurrentConfiguration != null)
        {
            _configService.CurrentConfiguration.TargetBoard = boardType;
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        ConnectionState = e.NewState;
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        UpdateConfigurationStats();
    }

    private void UpdateConfigurationStats()
    {
        var config = _configService.CurrentConfiguration;
        if (config != null)
        {
            InputCount = config.Inputs.Count;
            DisplayCount = config.Displays.Count;
            MappingCount = config.KeyboardMappings.Count;
            SelectedBoardType = config.TargetBoard;
        }
        else
        {
            InputCount = 0;
            DisplayCount = 0;
            MappingCount = 0;
        }
    }
}
