using System.Collections.ObjectModel;
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

    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;

    public ObservableCollection<ApplicationPackage> Packages { get; } = new();

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

    public int SelectedCount => Packages.Count(p => p.IsSelectedForRemoval);

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand SelectAllSafeCommand { get; }
    public IRelayCommand ClearSelectionCommand { get; }
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
        SelectAllSafeCommand = new RelayCommand(SelectAllSafe);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        PreviewRemovalPlanCommand = new AsyncRelayCommand(PreviewRemovalPlanAsync);
        DryRunSimulationCommand = new AsyncRelayCommand(DryRunSimulationAsync);
        ExecuteConfirmedPlanCommand = new AsyncRelayCommand(ExecuteConfirmedPlanAsync);
        CloseModalCommand = new RelayCommand(() => IsPlanModalOpen = false);
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

            ApplyFilter();

            StatusMessage = $"{Packages.Count} applications discovered.";
            OnPropertyChanged(nameof(SelectedCount));
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

    private void SelectAllSafe()
    {
        foreach (var p in Packages.Where(p => p.RiskLevel == OperationRiskLevel.Safe && !p.IsProtected))
        {
            p.IsSelectedForRemoval = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    private void ClearSelection()
    {
        foreach (var p in Packages)
        {
            p.IsSelectedForRemoval = false;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    private async Task PreviewRemovalPlanAsync()
    {
        var selected = Packages.Where(p => p.IsSelectedForRemoval).ToList();
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

        foreach (var item in query)
        {
            Packages.Add(item);
        }

        OnPropertyChanged(nameof(SelectedCount));
    }
}
