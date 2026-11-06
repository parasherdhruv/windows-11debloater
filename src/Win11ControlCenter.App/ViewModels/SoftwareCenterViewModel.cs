using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class SoftwareCenterViewModel : ViewModelBase
{
    private readonly IWingetService _wingetService;
    private readonly ILoggingService _logger;

    private List<SoftwarePackageItem> _allCatalog = new();
    private string _searchFilter = string.Empty;
    private string _selectedCategory = "All";
    private bool _isWingetAvailable = true;
    private bool _isInstalling;
    private string _installProgressStatus = string.Empty;

    public ObservableCollection<SoftwarePackageItem> Packages { get; } = new();
    public ObservableCollection<SoftwarePackageItem> SelectedPackages { get; } = new();

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

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsWingetAvailable
    {
        get => _isWingetAvailable;
        set => SetProperty(ref _isWingetAvailable, value);
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        set
        {
            if (SetProperty(ref _isInstalling, value))
            {
                OnPropertyChanged(nameof(IsNotInstalling));
            }
        }
    }

    public bool IsNotInstalling => !IsInstalling;

    public string InstallProgressStatus
    {
        get => _installProgressStatus;
        set => SetProperty(ref _installProgressStatus, value);
    }

    private IEnumerable<SoftwarePackageItem> GetEffectivePackages() => _allCatalog.Count > 0 ? _allCatalog : Packages;

    public int SelectedCount => GetEffectivePackages().Count(p => p.IsSelected);
    public bool HasSelectedPackages => SelectedCount > 0;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand InstallSelectedCommand { get; }
    public IAsyncRelayCommand<SoftwarePackageItem> InstallSingleCommand { get; }
    public IRelayCommand ClearSelectionCommand { get; }
    public IRelayCommand<SoftwarePackageItem> UnselectPackageCommand { get; }
    public IRelayCommand<string> FilterCategoryCommand { get; }
    public IRelayCommand<string> OpenHomepageCommand { get; }

    public SoftwareCenterViewModel(IWingetService wingetService, ILoggingService logger)
    {
        _wingetService = wingetService;
        _logger = logger;

        RefreshCommand = new AsyncRelayCommand(LoadPackagesAsync);
        InstallSelectedCommand = new AsyncRelayCommand(InstallSelectedAsync);
        InstallSingleCommand = new AsyncRelayCommand<SoftwarePackageItem>(InstallSingleAsync);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        UnselectPackageCommand = new RelayCommand<SoftwarePackageItem>(UnselectPackage);
        FilterCategoryCommand = new RelayCommand<string>(cat => { if (cat != null) SelectedCategory = cat; });
        OpenHomepageCommand = new RelayCommand<string>(OpenHomepage);

        Packages.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (SoftwarePackageItem item in e.NewItems)
                {
                    item.PropertyChanged -= OnPackagePropertyChanged;
                    item.PropertyChanged += OnPackagePropertyChanged;
                }
            }
            SyncSelectedCollection();
        };
    }

    public override async Task InitializeAsync()
    {
        await LoadPackagesAsync();
    }

    private async Task LoadPackagesAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking Winget package manager status...";

        try
        {
            IsWingetAvailable = await _wingetService.IsWingetAvailableAsync();

            StatusMessage = "Loading curated essential applications...";
            _allCatalog = await _wingetService.GetCuratedPackagesAsync();

            foreach (var pkg in _allCatalog)
            {
                pkg.PropertyChanged -= OnPackagePropertyChanged;
                pkg.PropertyChanged += OnPackagePropertyChanged;
            }

            ApplyFilter();
            IsBusy = false;

            if (IsWingetAvailable)
            {
                StatusMessage = $"Ready. {_allCatalog.Count} essential applications cataloged.";
                _ = Task.Run(async () =>
                {
                    await _wingetService.RefreshInstalledStatusAsync(_allCatalog);
                });
            }
            else
            {
                StatusMessage = "Winget (Windows Package Manager) is not detected in system PATH. Viewing catalog in offline mode.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load software catalog", ex, "SoftwareCenter");
            StatusMessage = $"Catalog error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoftwarePackageItem.IsSelected))
        {
            SyncSelectedCollection();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedPackages));
        }
    }

    private void SyncSelectedCollection()
    {
        var source = _allCatalog.Count > 0 ? _allCatalog : Packages.ToList();
        var selected = source.Where(p => p.IsSelected).ToList();

        SelectedPackages.Clear();
        foreach (var item in selected)
        {
            SelectedPackages.Add(item);
        }
    }

    private void ApplyFilter()
    {
        Packages.Clear();
        var query = _allCatalog.AsEnumerable();

        if (SelectedCategory != "All")
        {
            query = query.Where(p => string.Equals(p.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(p => p.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     p.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     p.Publisher.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     p.Id.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query.OrderBy(p => p.Name))
        {
            item.PropertyChanged -= OnPackagePropertyChanged;
            item.PropertyChanged += OnPackagePropertyChanged;
            Packages.Add(item);
        }

        SyncSelectedCollection();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
    }

    private void ClearSelection()
    {
        var source = _allCatalog.Count > 0 ? _allCatalog : Packages.ToList();
        foreach (var p in source)
        {
            p.IsSelected = false;
        }
        SyncSelectedCollection();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
    }

    private void UnselectPackage(SoftwarePackageItem? package)
    {
        if (package != null)
        {
            package.IsSelected = false;
            SyncSelectedCollection();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedPackages));
        }
    }

    private async Task InstallSingleAsync(SoftwarePackageItem? package)
    {
        if (package == null || IsInstalling) return;

        IsInstalling = true;
        InstallProgressStatus = $"Installing {package.Name}...";

        try
        {
            var progress = new Progress<string>(msg => InstallProgressStatus = msg);
            await _wingetService.InstallPackageAsync(package, progress);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private async Task InstallSelectedAsync()
    {
        var selected = GetEffectivePackages()
            .Where(p => p.IsSelected && !p.IsInstalled)
            .ToList();

        if (selected.Count == 0)
        {
            StatusMessage = "No uninstalled packages selected.";
            return;
        }

        IsInstalling = true;
        InstallProgressStatus = $"Starting installation of {selected.Count} selected applications...";

        try
        {
            int index = 0;
            foreach (var pkg in selected)
            {
                index++;
                InstallProgressStatus = $"[{index}/{selected.Count}] Installing {pkg.Name}...";
                var progress = new Progress<string>(msg => InstallProgressStatus = $"[{index}/{selected.Count}] {msg}");
                await _wingetService.InstallPackageAsync(pkg, progress);
            }

            InstallProgressStatus = $"Installation batch complete! {selected.Count(p => p.IsInstalled)} packages installed.";
            await _wingetService.RefreshInstalledStatusAsync(_allCatalog);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void OpenHomepage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open homepage '{url}'", ex, "SoftwareCenter");
        }
    }
}
