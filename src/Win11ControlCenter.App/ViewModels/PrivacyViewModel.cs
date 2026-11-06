using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class PrivacyViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IPrivacyService _privacyService;
    private readonly IOperationExecutor _operationExecutor;

    private string _searchFilter = string.Empty;
    private string _selectedCategory = "All";
    private List<PrivacyTweak> _allTweaks = new();

    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;
    private bool _isDryRun;

    public ObservableCollection<PrivacyTweak> Tweaks { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

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

    public int TotalCount => _allTweaks.Count;
    public int AppliedCount => _allTweaks.Count(t => t.IsApplied);
    public int UnappliedCount => _allTweaks.Count(t => !t.IsApplied);

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

    public bool IsDryRun
    {
        get => _isDryRun;
        set => SetProperty(ref _isDryRun, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ApplyBaselineCommand { get; }
    public IAsyncRelayCommand RevertAllCommand { get; }
    public IAsyncRelayCommand<PrivacyTweak> ToggleTweakCommand { get; }
    public IAsyncRelayCommand ExecutePlanCommand { get; }
    public IRelayCommand CancelPlanCommand { get; }

    public PrivacyViewModel(ILoggingService logger, IPrivacyService privacyService, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _privacyService = privacyService;
        _operationExecutor = operationExecutor;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ApplyBaselineCommand = new AsyncRelayCommand(ApplyBaselineAsync);
        RevertAllCommand = new AsyncRelayCommand(RevertAllAsync);
        ToggleTweakCommand = new AsyncRelayCommand<PrivacyTweak>(ToggleTweakAsync);
        ExecutePlanCommand = new AsyncRelayCommand(ExecuteActivePlanAsync);
        CancelPlanCommand = new RelayCommand(() => IsPlanModalOpen = false);

        Categories.Add("All");
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var tweaks = await _privacyService.GetPrivacyTweaksAsync();
            _allTweaks = tweaks.ToList();

            var uniqueCategories = _allTweaks
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            Categories.Add("All");
            foreach (var cat in uniqueCategories)
            {
                Categories.Add(cat);
            }

            ApplyFilter();
            NotifyStats();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to scan privacy settings", ex, "PrivacyViewModel");
        }
    }

    private void ApplyFilter()
    {
        Tweaks.Clear();
        var query = _allTweaks.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
        {
            query = query.Where(t => t.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            var search = SearchFilter.Trim();
            query = query.Where(t =>
                t.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var tweak in query)
        {
            Tweaks.Add(tweak);
        }
    }

    private void NotifyStats()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(AppliedCount));
        OnPropertyChanged(nameof(UnappliedCount));
    }

    private async Task ApplyBaselineAsync()
    {
        var unapplied = _allTweaks.Where(t => !t.IsApplied).ToList();
        if (unapplied.Count == 0)
        {
            _logger.LogInfo("All recommended privacy settings are already applied.", "PrivacyViewModel");
            return;
        }

        IsDryRun = false;
        ActivePlan = await _privacyService.CreatePrivacyPlanAsync(unapplied, IsDryRun);
        IsPlanModalOpen = true;
    }

    private async Task RevertAllAsync()
    {
        var applied = _allTweaks.Where(t => t.IsApplied).ToList();
        if (applied.Count == 0)
        {
            _logger.LogInfo("No privacy settings are currently applied to revert.", "PrivacyViewModel");
            return;
        }

        IsDryRun = false;
        ActivePlan = await _privacyService.CreateRevertPlanAsync(applied, IsDryRun);
        IsPlanModalOpen = true;
    }

    private async Task ToggleTweakAsync(PrivacyTweak? tweak)
    {
        if (tweak == null) return;

        IsDryRun = false;
        if (tweak.IsApplied)
        {
            ActivePlan = await _privacyService.CreateRevertPlanAsync(new[] { tweak }, IsDryRun);
        }
        else
        {
            ActivePlan = await _privacyService.CreatePrivacyPlanAsync(new[] { tweak }, IsDryRun);
        }

        IsPlanModalOpen = true;
    }

    private async Task ExecuteActivePlanAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressPercent = 0;
        ProgressStatus = "Preparing execution...";

        ActivePlan.IsDryRun = IsDryRun;

        var progress = new Progress<OperationProgress>(p =>
        {
            ProgressPercent = p.PercentComplete;
            ProgressStatus = $"{p.StatusMessage} ({p.CurrentIndex}/{p.TotalCount})";
        });

        try
        {
            await _operationExecutor.ExecutePlanAsync(ActivePlan, progress);
            await RefreshAsync();
            ProgressStatus = IsDryRun ? "Dry-run simulation completed." : "Privacy baseline applied successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError("Execution error during privacy plan", ex, "PrivacyViewModel");
            ProgressStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
