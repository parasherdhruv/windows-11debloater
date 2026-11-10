using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class SystemCleanerViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly ICleanupService _cleanupService;
    private readonly IOperationExecutor _operationExecutor;

    private List<CleanupTarget> _allTargets = new();
    private string _activeViewTab = "All"; // "All" or "Selected"
    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;
    private bool _isDryRun;
    private string _lastCleanResult = string.Empty;

    public ObservableCollection<CleanupTarget> Targets { get; } = new();
    public ObservableCollection<CleanupTarget> SelectedTargets { get; } = new();

    public string ActiveViewTab
    {
        get => _activeViewTab;
        set
        {
            if (SetProperty(ref _activeViewTab, value))
            {
                OnPropertyChanged(nameof(IsAllTabActive));
                OnPropertyChanged(nameof(IsSelectedTabActive));
                ApplyFilter();
            }
        }
    }

    public bool IsAllTabActive
    {
        get => ActiveViewTab == "All";
        set { if (value) ActiveViewTab = "All"; }
    }

    public bool IsSelectedTabActive
    {
        get => ActiveViewTab == "Selected";
        set { if (value) ActiveViewTab = "Selected"; }
    }

    private IEnumerable<CleanupTarget> GetEffectiveTargets() => _allTargets.Count > 0 ? _allTargets : Targets;

    public long TotalScannedBytes => GetEffectiveTargets().Sum(t => t.TotalSizeBytes);
    public long TotalSelectedBytes => GetEffectiveTargets().Where(t => t.IsSelected).Sum(t => t.TotalSizeBytes);
    public int TotalSelectedFiles => GetEffectiveTargets().Where(t => t.IsSelected).Sum(t => t.FileCount);
    public int SelectedCount => GetEffectiveTargets().Count(t => t.IsSelected);
    public bool HasSelectedTargets => SelectedCount > 0;

    public string TotalScannedFormatted => FormatSize(TotalScannedBytes);
    public string TotalSelectedFormatted => FormatSize(TotalSelectedBytes);

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

    public string LastCleanResult
    {
        get => _lastCleanResult;
        set => SetProperty(ref _lastCleanResult, value);
    }

    public IAsyncRelayCommand ScanCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IRelayCommand DeselectAllCommand { get; }
    public IRelayCommand<CleanupTarget> UnselectTargetCommand { get; }
    public IRelayCommand<string> SwitchTabCommand { get; }
    public IAsyncRelayCommand CleanSelectedCommand { get; }
    public IAsyncRelayCommand ExecuteCleanCommand { get; }
    public IRelayCommand CancelModalCommand { get; }

    public SystemCleanerViewModel(
        ILoggingService logger,
        ICleanupService cleanupService,
        IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _cleanupService = cleanupService;
        _operationExecutor = operationExecutor;

        ScanCommand = new AsyncRelayCommand(ScanAsync);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        UnselectTargetCommand = new RelayCommand<CleanupTarget>(UnselectTarget);
        SwitchTabCommand = new RelayCommand<string>(tab => { if (tab != null) ActiveViewTab = tab; });
        CleanSelectedCommand = new AsyncRelayCommand(PrepareCleanupPlanAsync);
        ExecuteCleanCommand = new AsyncRelayCommand(ExecuteActivePlanAsync);
        CancelModalCommand = new RelayCommand(() => IsPlanModalOpen = false);

        Targets.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (CleanupTarget item in e.NewItems)
                {
                    item.PropertyChanged -= OnTargetPropertyChanged;
                    item.PropertyChanged += OnTargetPropertyChanged;
                }
            }
            SyncSelectedCollection();
        };
    }

    public override async Task InitializeAsync()
    {
        await ScanAsync();
    }

    private async Task ScanAsync()
    {
        try
        {
            ProgressStatus = "Scanning temporary directories and recycle bin...";
            var scanned = await _cleanupService.ScanCleanupTargetsAsync();
            _allTargets = scanned.ToList();

            foreach (var t in _allTargets)
            {
                t.PropertyChanged -= OnTargetPropertyChanged;
                t.PropertyChanged += OnTargetPropertyChanged;
            }

            ApplyFilter();
            _logger.LogInfo($"Storage scan complete. Reclaimable: {TotalScannedFormatted}", "SystemCleaner");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed during system storage scan", ex, "SystemCleaner");
        }
    }

    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupTarget.IsSelected))
        {
            SyncSelectedCollection();
            NotifyTotals();
            if (ActiveViewTab == "Selected")
            {
                ApplyFilter();
            }
        }
    }

    private void SyncSelectedCollection()
    {
        var source = _allTargets.Count > 0 ? _allTargets : Targets.ToList();
        var selected = source.Where(t => t.IsSelected).ToList();

        SelectedTargets.Clear();
        foreach (var item in selected)
        {
            SelectedTargets.Add(item);
        }
    }

    private void ApplyFilter()
    {
        Targets.Clear();
        var query = _allTargets.AsEnumerable();

        if (ActiveViewTab == "Selected")
        {
            query = query.Where(t => t.IsSelected);
        }

        foreach (var item in query)
        {
            item.PropertyChanged -= OnTargetPropertyChanged;
            item.PropertyChanged += OnTargetPropertyChanged;
            Targets.Add(item);
        }

        SyncSelectedCollection();
        NotifyTotals();
    }

    private void SelectAll()
    {
        var source = _allTargets.Count > 0 ? _allTargets : Targets.ToList();
        foreach (var t in source)
        {
            t.IsSelected = true;
        }
        SyncSelectedCollection();
        NotifyTotals();
    }

    private void DeselectAll()
    {
        var source = _allTargets.Count > 0 ? _allTargets : Targets.ToList();
        foreach (var t in source)
        {
            t.IsSelected = false;
        }
        SyncSelectedCollection();
        NotifyTotals();

        if (ActiveViewTab == "Selected")
        {
            ApplyFilter();
        }
    }

    private void UnselectTarget(CleanupTarget? target)
    {
        if (target != null)
        {
            target.IsSelected = false;
            SyncSelectedCollection();
            NotifyTotals();

            if (ActiveViewTab == "Selected")
            {
                ApplyFilter();
            }
        }
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(TotalScannedBytes));
        OnPropertyChanged(nameof(TotalSelectedBytes));
        OnPropertyChanged(nameof(TotalSelectedFiles));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedTargets));
        OnPropertyChanged(nameof(TotalScannedFormatted));
        OnPropertyChanged(nameof(TotalSelectedFormatted));
    }

    private async Task PrepareCleanupPlanAsync()
    {
        var source = _allTargets.Count > 0 ? _allTargets : Targets.ToList();
        var selected = source.Where(t => t.IsSelected && (t.FileCount > 0 || t.TotalSizeBytes > 0 || t.TargetPath.Equals("RecycleBin", StringComparison.OrdinalIgnoreCase))).ToList();
        if (selected.Count == 0)
        {
            _logger.LogInfo("No cleanup targets with files selected.", "SystemCleaner");
            return;
        }

        IsDryRun = false;
        ActivePlan = await _cleanupService.CreateCleanupPlanAsync(selected, IsDryRun);
        IsPlanModalOpen = true;
    }

    private async Task ExecuteActivePlanAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressPercent = 0;
        ProgressStatus = IsDryRun ? "Simulating cleanup plan..." : "Executing cleanup plan...";

        try
        {
            ActivePlan.IsDryRun = IsDryRun;
            var progress = new Progress<OperationProgress>(p =>
            {
                ProgressPercent = p.PercentComplete;
                ProgressStatus = $"{p.CurrentOperationName} ({p.CurrentIndex}/{p.TotalCount})";
            });

            await _operationExecutor.ExecutePlanAsync(ActivePlan, progress);

            LastCleanResult = $"Cleaned {ActivePlan.SucceededCount} categories successfully. " +
                              $"{(ActivePlan.FailedCount > 0 ? $"{ActivePlan.FailedCount} targets skipped." : "")}";

            // Rescan to show updated sizes
            await ScanAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Cleanup execution failed", ex, "SystemCleaner");
            ProgressStatus = $"Execution failed: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 MB";
        double mb = bytes / (1024.0 * 1024.0);
        return mb < 1024 ? $"{mb:F1} MB" : $"{mb / 1024.0:F2} GB";
    }
}
