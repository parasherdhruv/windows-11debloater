using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class SystemCleanerViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly ICleanupService _cleanupService;
    private readonly IOperationExecutor _operationExecutor;

    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;
    private bool _isDryRun;
    private string _lastCleanResult = string.Empty;

    public ObservableCollection<CleanupTarget> Targets { get; } = new();

    public long TotalScannedBytes => Targets.Sum(t => t.TotalSizeBytes);
    public long TotalSelectedBytes => Targets.Where(t => t.IsSelected).Sum(t => t.TotalSizeBytes);
    public int TotalSelectedFiles => Targets.Where(t => t.IsSelected).Sum(t => t.FileCount);

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
        CleanSelectedCommand = new AsyncRelayCommand(PrepareCleanupPlanAsync);
        ExecuteCleanCommand = new AsyncRelayCommand(ExecuteActivePlanAsync);
        CancelModalCommand = new RelayCommand(() => IsPlanModalOpen = false);
    }

    public override async Task InitializeAsync()
    {
        await ScanAsync();
    }

    public void NotifyTargetSelectionChanged()
    {
        OnPropertyChanged(nameof(TotalSelectedBytes));
        OnPropertyChanged(nameof(TotalSelectedFiles));
        OnPropertyChanged(nameof(TotalSelectedFormatted));
    }

    private async Task ScanAsync()
    {
        try
        {
            ProgressStatus = "Scanning temporary directories and recycle bin...";
            var scanned = await _cleanupService.ScanCleanupTargetsAsync();

            Targets.Clear();
            foreach (var target in scanned)
            {
                Targets.Add(target);
            }

            NotifyTotals();
            _logger.LogInfo($"Storage scan complete. Reclaimable: {TotalScannedFormatted}", "SystemCleaner");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed during system storage scan", ex, "SystemCleaner");
        }
    }

    private void SelectAll()
    {
        foreach (var t in Targets)
        {
            t.IsSelected = true;
        }
        NotifyTotals();
    }

    private void DeselectAll()
    {
        foreach (var t in Targets)
        {
            t.IsSelected = false;
        }
        NotifyTotals();
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(TotalScannedBytes));
        OnPropertyChanged(nameof(TotalSelectedBytes));
        OnPropertyChanged(nameof(TotalSelectedFiles));
        OnPropertyChanged(nameof(TotalScannedFormatted));
        OnPropertyChanged(nameof(TotalSelectedFormatted));
    }

    private async Task PrepareCleanupPlanAsync()
    {
        var selected = Targets.Where(t => t.IsSelected && t.FileCount > 0).ToList();
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
        ProgressStatus = "Starting cleanup...";

        ActivePlan.IsDryRun = IsDryRun;

        var progress = new Progress<OperationProgress>(p =>
        {
            ProgressPercent = p.PercentComplete;
            ProgressStatus = $"{p.StatusMessage} ({p.CurrentIndex}/{p.TotalCount})";
        });

        try
        {
            await _operationExecutor.ExecutePlanAsync(ActivePlan, progress);
            LastCleanResult = IsDryRun
                ? "Dry-run simulation completed. No files were removed."
                : $"Cleanup completed successfully! Reclaimed {TotalSelectedFormatted}.";

            // Re-scan after deletion to reflect new reclaimed sizes
            await ScanAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Execution error during file cleanup", ex, "SystemCleaner");
            LastCleanResult = $"Error: {ex.Message}";
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

