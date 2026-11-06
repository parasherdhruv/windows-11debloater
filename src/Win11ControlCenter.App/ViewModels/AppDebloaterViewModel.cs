using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class AppDebloaterViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IPackageManagerService _packageManager;
    private readonly IOperationExecutor _operationExecutor;

    private string _searchFilter = string.Empty;
    private PackageCategory? _selectedCategory;
    private ApplicationPackage? _selectedPackage;
    private List<ApplicationPackage> _allDiscoveredPackages = new();
    private string _activeViewTab = "All"; // "All", "Selected", "SafeBloat"
    private bool _pinSelectedToTop = true;

    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;

    public ObservableCollection<ApplicationPackage> Packages { get; } = new();
    public ObservableCollection<ApplicationPackage> SelectedPackages { get; } = new();

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

    public PackageCategory? SelectedCategory
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

    public string ActiveViewTab
    {
        get => _activeViewTab;
        set
        {
            if (SetProperty(ref _activeViewTab, value))
            {
                OnPropertyChanged(nameof(IsAllTabActive));
                OnPropertyChanged(nameof(IsSelectedTabActive));
                OnPropertyChanged(nameof(IsSafeBloatTabActive));
                ApplyFilter();
            }
        }
    }

    public bool IsAllTabActive => ActiveViewTab == "All";
    public bool IsSelectedTabActive => ActiveViewTab == "Selected";
    public bool IsSafeBloatTabActive => ActiveViewTab == "SafeBloat";

    public bool PinSelectedToTop
    {
        get => _pinSelectedToTop;
        set
        {
            if (SetProperty(ref _pinSelectedToTop, value))
            {
                ApplyFilter();
            }
        }
    }

    public ApplicationPackage? SelectedPackage
    {
        get => _selectedPackage;
        set => SetProperty(ref _selectedPackage, value);
    }

    public bool IsPlanModalOpen
    {
        get => _isPlanModalOpen;
        set => SetProperty(ref _isPlanModalOpen, value);
    }

    public OperationPlan? ActivePlan
    {
        get => _activePlan;
        set => SetProperty(ref _activePlan, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set => SetProperty(ref _progressPercent, value);
    }

    public string ProgressStatus
    {
        get => _progressStatus;
        set => SetProperty(ref _progressStatus, value);
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                OnPropertyChanged(nameof(IsNotExecuting));
            }
        }
    }

    public bool IsNotExecuting => !IsExecuting;

    public int SelectedCount => _allDiscoveredPackages.Count > 0
        ? _allDiscoveredPackages.Count(p => p.IsSelectedForRemoval)
        : Packages.Count(p => p.IsSelectedForRemoval);

    public bool HasSelectedPackages => SelectedCount > 0;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand QuickDebloatCommand { get; }
    public IRelayCommand SelectAllSafeCommand { get; }
    public IRelayCommand ClearSelectionCommand { get; }
    public IRelayCommand<ApplicationPackage> UnselectPackageCommand { get; }
    public IRelayCommand<string> SwitchTabCommand { get; }
    public IAsyncRelayCommand PreviewRemovalPlanCommand { get; }
    public IAsyncRelayCommand DryRunSimulationCommand { get; }
    public IAsyncRelayCommand ExecuteConfirmedPlanCommand { get; }
    public IRelayCommand CloseModalCommand { get; }

    public AppDebloaterViewModel(ILoggingService logger, IPackageManagerService packageManager, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _packageManager = packageManager;
        _operationExecutor = operationExecutor;

        RefreshCommand = new AsyncRelayCommand(LoadPackagesAsync);
        QuickDebloatCommand = new AsyncRelayCommand(QuickDebloatAsync);
        SelectAllSafeCommand = new RelayCommand(SelectAllSafe);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        UnselectPackageCommand = new RelayCommand<ApplicationPackage>(UnselectPackage);
        SwitchTabCommand = new RelayCommand<string>(tab => { if (tab != null) ActiveViewTab = tab; });
        PreviewRemovalPlanCommand = new AsyncRelayCommand(PreviewRemovalPlanAsync);
        DryRunSimulationCommand = new AsyncRelayCommand(DryRunSimulationAsync);
        ExecuteConfirmedPlanCommand = new AsyncRelayCommand(ExecuteConfirmedPlanAsync);
        CloseModalCommand = new RelayCommand(() => IsPlanModalOpen = false);

        Packages.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (ApplicationPackage item in e.NewItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                    item.PropertyChanged += OnItemPropertyChanged;
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
        StatusMessage = "Scanning installed and provisioned applications...";

        try
        {
            var discovered = await _packageManager.GetInstalledPackagesAsync();
            _allDiscoveredPackages = discovered.ToList();

            foreach (var item in _allDiscoveredPackages)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.PropertyChanged += OnItemPropertyChanged;
            }

            ApplyFilter();

            StatusMessage = $"{_allDiscoveredPackages.Count} applications discovered.";
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedPackages));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan error: {ex.Message}";
            _logger.LogError("Package discovery failed", ex, "AppDebloater");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ApplicationPackage.IsSelectedForRemoval))
        {
            SyncSelectedCollection();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedPackages));

            if (ActiveViewTab == "Selected")
            {
                ApplyFilter();
            }
        }
    }

    private void SyncSelectedCollection()
    {
        var source = _allDiscoveredPackages.Count > 0 ? _allDiscoveredPackages : Packages.ToList();
        var selected = source.Where(p => p.IsSelectedForRemoval).ToList();

        SelectedPackages.Clear();
        foreach (var item in selected)
        {
            SelectedPackages.Add(item);
        }
    }

    private void SelectAllSafe()
    {
        var source = _allDiscoveredPackages.Count > 0 ? _allDiscoveredPackages : Packages.ToList();
        foreach (var p in source.Where(p => p.RiskLevel == OperationRiskLevel.Safe && !p.IsProtected))
        {
            p.IsSelectedForRemoval = true;
        }
        SyncSelectedCollection();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
    }

    private async Task QuickDebloatAsync()
    {
        SelectAllSafe();
        if (SelectedCount > 0)
        {
            await PreviewRemovalPlanAsync();
        }
        else
        {
            StatusMessage = "No safe bloatware packages detected on this system.";
        }
    }

    private void ClearSelection()
    {
        var source = _allDiscoveredPackages.Count > 0 ? _allDiscoveredPackages : Packages.ToList();
        foreach (var p in source)
        {
            p.IsSelectedForRemoval = false;
        }
        SyncSelectedCollection();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));

        if (ActiveViewTab == "Selected")
        {
            ApplyFilter();
        }
    }

    private void UnselectPackage(ApplicationPackage? package)
    {
        if (package != null)
        {
            package.IsSelectedForRemoval = false;
            SyncSelectedCollection();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedPackages));

            if (ActiveViewTab == "Selected")
            {
                ApplyFilter();
            }
        }
    }

    private async Task PreviewRemovalPlanAsync()
    {
        var source = _allDiscoveredPackages.Count > 0 ? _allDiscoveredPackages : Packages.ToList();
        var selected = source.Where(p => p.IsSelectedForRemoval).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "No packages selected for removal. Select applications first.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Analyzing dependencies and preparing removal plan...";

        try
        {
            ActivePlan = await _packageManager.CreateRemovalPlanAsync(selected, isDryRun: false);
            IsPlanModalOpen = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DryRunSimulationAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressStatus = "Starting dry-run simulation...";
        ProgressPercent = 0;

        try
        {
            ActivePlan.IsDryRun = true;
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressPercent = p.PercentComplete;
                ProgressStatus = $"[Dry Run] {p.CurrentOperationName} ({p.CurrentIndex}/{p.TotalCount})";
            });

            await _operationExecutor.ExecutePlanAsync(ActivePlan, progress);
            ProgressStatus = "Dry-run simulation completed. No system changes were made.";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private async Task ExecuteConfirmedPlanAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressStatus = "Executing removal plan...";
        ProgressPercent = 0;

        try
        {
            ActivePlan.IsDryRun = false;
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressPercent = p.PercentComplete;
                ProgressStatus = $"{p.CurrentOperationName} ({p.CurrentIndex}/{p.TotalCount})";
            });

            await _operationExecutor.ExecutePlanAsync(ActivePlan, progress);
            ProgressStatus = $"Completed. {ActivePlan.SucceededCount} succeeded, {ActivePlan.FailedCount} failed.";

            // Reload packages to reflect removals
            await LoadPackagesAsync();
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private void ApplyFilter()
    {
        Packages.Clear();
        var query = _allDiscoveredPackages.AsEnumerable();

        if (ActiveViewTab == "Selected")
        {
            query = query.Where(p => p.IsSelectedForRemoval);
        }
        else if (ActiveViewTab == "SafeBloat")
        {
            query = query.Where(p => p.RiskLevel == OperationRiskLevel.Safe && !p.IsProtected);
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(p => p.DisplayName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     p.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                     p.Publisher.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCategory.HasValue)
        {
            query = query.Where(p => p.Category == SelectedCategory.Value);
        }

        if (PinSelectedToTop && ActiveViewTab == "All")
        {
            query = query.OrderByDescending(p => p.IsSelectedForRemoval)
                         .ThenBy(p => p.DisplayName);
        }
        else
        {
            query = query.OrderBy(p => p.DisplayName);
        }

        foreach (var item in query)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            item.PropertyChanged += OnItemPropertyChanged;
            Packages.Add(item);
        }

        SyncSelectedCollection();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedPackages));
    }
}
