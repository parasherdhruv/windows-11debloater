using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;

namespace Win11ControlCenter.App.ViewModels;

public partial class StartupManagerViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IStartupManagerService _startupManager;
    private string _searchFilter = string.Empty;
    private StartupEntry? _selectedItem;
    private List<StartupEntry> _allDiscoveredEntries = new();

    public ObservableCollection<StartupEntry> StartupItems { get; } = new();

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public StartupEntry? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<StartupEntry> ToggleStateCommand { get; }
    public IRelayCommand<StartupEntry> OpenLocationCommand { get; }
    public IAsyncRelayCommand ExportInventoryCommand { get; }

    public StartupManagerViewModel(ILoggingService logger, IStartupManagerService startupManager)
    {
        _logger = logger;
        _startupManager = startupManager;
        RefreshCommand = new AsyncRelayCommand(LoadStartupItemsAsync);
        ToggleStateCommand = new AsyncRelayCommand<StartupEntry>(ToggleStateAsync);
        OpenLocationCommand = new RelayCommand<StartupEntry>(OpenLocation);
        ExportInventoryCommand = new AsyncRelayCommand(ExportInventoryAsync);
    }

    public override async Task InitializeAsync() => await LoadStartupItemsAsync();

    private async Task LoadStartupItemsAsync()
    {
        IsBusy = true;
        StatusMessage = "Auditing Windows startup applications...";

        try
        {
            var items = await _startupManager.GetStartupEntriesAsync();
            _allDiscoveredEntries = items.ToList();

            ApplyFilter();

            StatusMessage = $"{StartupItems.Count} startup application(s) discovered.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Startup scan failed: {ex.Message}";
            _logger.LogError("Startup discovery failed", ex, "StartupManager");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleStateAsync(StartupEntry? entry)
    {
        if (entry == null) return;

        var newState = !entry.IsEnabled;
        StatusMessage = $"{(newState ? "Enabling" : "Disabling")} {entry.Name}...";

        var ok = await _startupManager.SetStartupEntryStateAsync(entry, newState);
        if (ok)
        {
            StatusMessage = $"Startup entry '{entry.Name}' is now {(newState ? "enabled" : "disabled")}.";
        }
        else
        {
            StatusMessage = $"Could not alter startup state for '{entry.Name}'.";
        }
    }

    private void OpenLocation(StartupEntry? entry)
    {
        if (entry != null)
        {
            _startupManager.OpenFileLocation(entry);
        }
    }

    private async Task ExportInventoryAsync()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var exportPath = System.IO.Path.Combine(localAppData, "Win11ControlCenter", "Exports", $"startup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        try
        {
            await _startupManager.ExportStartupInventoryAsync(_allDiscoveredEntries, exportPath);
            StatusMessage = $"Exported to: {exportPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        StartupItems.Clear();
        var query = _allDiscoveredEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(e => e.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     e.Command.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     e.Publisher.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            StartupItems.Add(item);
        }
    }
}

public partial class ServicesManagerViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IServiceManagerService _serviceManager;
    private string _searchFilter = string.Empty;
    private WindowsServiceItem? _selectedService;
    private List<WindowsServiceItem> _allDiscoveredServices = new();

    public ObservableCollection<WindowsServiceItem> Services { get; } = new();

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public WindowsServiceItem? SelectedService
    {
        get => _selectedService;
        set => SetProperty(ref _selectedService, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<string> SetStartupTypeCommand { get; }
    public IAsyncRelayCommand RestoreStartupTypeCommand { get; }
    public IAsyncRelayCommand StartSelectedServiceCommand { get; }
    public IAsyncRelayCommand StopSelectedServiceCommand { get; }
    public IAsyncRelayCommand ExportInventoryCommand { get; }

    public ServicesManagerViewModel(ILoggingService logger, IServiceManagerService serviceManager)
    {
        _logger = logger;
        _serviceManager = serviceManager;

        RefreshCommand = new AsyncRelayCommand(LoadServicesAsync);
        SetStartupTypeCommand = new AsyncRelayCommand<string>(SetStartupTypeAsync);
        RestoreStartupTypeCommand = new AsyncRelayCommand(RestoreStartupTypeAsync);
        StartSelectedServiceCommand = new AsyncRelayCommand(StartSelectedServiceAsync);
        StopSelectedServiceCommand = new AsyncRelayCommand(StopSelectedServiceAsync);
        ExportInventoryCommand = new AsyncRelayCommand(ExportInventoryAsync);
    }

    public override async Task InitializeAsync() => await LoadServicesAsync();

    private async Task LoadServicesAsync()
    {
        IsBusy = true;
        StatusMessage = "Enumerating registered Windows services...";

        try
        {
            var services = await _serviceManager.GetServicesAsync();
            _allDiscoveredServices = services.ToList();

            ApplyFilter();

            StatusMessage = $"{Services.Count} Windows service(s) cataloged.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Service scan failed: {ex.Message}";
            _logger.LogError("Services scan failed", ex, "ServicesManager");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SetStartupTypeAsync(string? newType)
    {
        if (SelectedService == null || string.IsNullOrWhiteSpace(newType)) return;

        if (SelectedService.IsCoreService)
        {
            StatusMessage = $"Safety shield active: Service '{SelectedService.ServiceName}' is a protected core Windows component and cannot be changed.";
            return;
        }

        StatusMessage = $"Setting startup type for '{SelectedService.ServiceName}' to {newType}...";
        var ok = await _serviceManager.SetStartupTypeAsync(SelectedService.ServiceName, newType);
        if (ok)
        {
            SelectedService.StartupType = newType;
            StatusMessage = $"Service '{SelectedService.ServiceName}' startup type configured to {newType}.";
        }
        else
        {
            StatusMessage = $"Failed to change startup type for '{SelectedService.ServiceName}'. Ensure administrator rights.";
        }
    }

    private async Task RestoreStartupTypeAsync()
    {
        if (SelectedService == null) return;
        await SetStartupTypeAsync(SelectedService.OriginalStartupType);
    }

    private async Task StartSelectedServiceAsync()
    {
        if (SelectedService == null) return;
        StatusMessage = $"Starting '{SelectedService.ServiceName}'...";
        var ok = await _serviceManager.StartServiceAsync(SelectedService.ServiceName);
        if (ok)
        {
            SelectedService.Status = "Running";
            StatusMessage = $"Service '{SelectedService.ServiceName}' is now running.";
        }
    }

    private async Task StopSelectedServiceAsync()
    {
        if (SelectedService == null) return;
        if (SelectedService.IsCoreService)
        {
            StatusMessage = $"Safety shield active: Core Windows service '{SelectedService.ServiceName}' cannot be stopped.";
            return;
        }

        StatusMessage = $"Stopping '{SelectedService.ServiceName}'...";
        var ok = await _serviceManager.StopServiceAsync(SelectedService.ServiceName);
        if (ok)
        {
            SelectedService.Status = "Stopped";
            StatusMessage = $"Service '{SelectedService.ServiceName}' has stopped.";
        }
    }

    private async Task ExportInventoryAsync()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var exportPath = System.IO.Path.Combine(localAppData, "Win11ControlCenter", "Exports", $"services_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        try
        {
            await _serviceManager.ExportServicesInventoryAsync(_allDiscoveredServices, exportPath);
            StatusMessage = $"Exported to: {exportPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        Services.Clear();
        var query = _allDiscoveredServices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(s => s.DisplayName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     s.ServiceName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     s.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            Services.Add(item);
        }
    }
}
