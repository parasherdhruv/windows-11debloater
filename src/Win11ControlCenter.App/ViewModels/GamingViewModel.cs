using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class GamingViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly ITweakEngineService _tweakService;
    private readonly ISystemDiscoveryService _discoveryService;
    private readonly IOperationExecutor _operationExecutor;

    private string _gpuName = "Detecting GPU...";
    private string _cpuName = "Detecting CPU...";
    private string _ramAmount = "16 GB";
    private string _activePowerPlanName = "Balanced";
    private bool _isGameModeActive;
    private bool _isHagsActive;
    private bool _isGameDvrDisabled;

    private bool _isPlanModalOpen;
    private OperationPlan? _activePlan;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;
    private bool _isDryRun;

    public ObservableCollection<OptimizationTweak> GamingTweaks { get; } = new();
    public ObservableCollection<PowerPlanInfo> PowerPlans { get; } = new();

    public string GpuName
    {
        get => _gpuName;
        set => SetProperty(ref _gpuName, value);
    }

    public string CpuName
    {
        get => _cpuName;
        set => SetProperty(ref _cpuName, value);
    }

    public string RamAmount
    {
        get => _ramAmount;
        set => SetProperty(ref _ramAmount, value);
    }

    public string ActivePowerPlanName
    {
        get => _activePowerPlanName;
        set => SetProperty(ref _activePowerPlanName, value);
    }

    public bool IsGameModeActive
    {
        get => _isGameModeActive;
        set => SetProperty(ref _isGameModeActive, value);
    }

    public bool IsHagsActive
    {
        get => _isHagsActive;
        set => SetProperty(ref _isHagsActive, value);
    }

    public bool IsGameDvrDisabled
    {
        get => _isGameDvrDisabled;
        set => SetProperty(ref _isGameDvrDisabled, value);
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

    public bool IsDryRun
    {
        get => _isDryRun;
        set => SetProperty(ref _isDryRun, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<OptimizationTweak> ToggleTweakCommand { get; }
    public IAsyncRelayCommand OptimizeAllGamingCommand { get; }
    public IAsyncRelayCommand<PowerPlanInfo> ActivatePowerPlanCommand { get; }
    public IAsyncRelayCommand ExecutePlanCommand { get; }
    public IRelayCommand CancelModalCommand { get; }

    public GamingViewModel(
        ILoggingService logger,
        ITweakEngineService tweakService,
        ISystemDiscoveryService discoveryService,
        IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _tweakService = tweakService;
        _discoveryService = discoveryService;
        _operationExecutor = operationExecutor;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ToggleTweakCommand = new AsyncRelayCommand<OptimizationTweak>(ToggleTweakAsync);
        OptimizeAllGamingCommand = new AsyncRelayCommand(OptimizeAllGamingAsync);
        ActivatePowerPlanCommand = new AsyncRelayCommand<PowerPlanInfo>(ActivatePowerPlanAsync);
        ExecutePlanCommand = new AsyncRelayCommand(ExecuteActivePlanAsync);
        CancelModalCommand = new RelayCommand(() => IsPlanModalOpen = false);
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            // 1. Hardware info
            var sysInfo = await _discoveryService.GetSystemInfoAsync();
            GpuName = sysInfo.GpuModel;
            CpuName = sysInfo.CpuModel;
            RamAmount = $"{sysInfo.TotalRamGigabytes:F1} GB";

            // 2. Gaming Tweaks
            GamingTweaks.Clear();
            var tweaks = await _tweakService.GetGamingTweaksAsync();
            foreach (var tweak in tweaks)
            {
                GamingTweaks.Add(tweak);

                if (tweak.Id == "enable-game-mode")
                    IsGameModeActive = tweak.IsApplied;
                else if (tweak.Id == "enable-hags")
                    IsHagsActive = tweak.IsApplied;
                else if (tweak.Id == "disable-game-dvr-background-recording")
                    IsGameDvrDisabled = tweak.IsApplied;
            }

            // 3. Power plans
            PowerPlans.Clear();
            var plans = await _tweakService.GetPowerPlansAsync();
            foreach (var p in plans)
            {
                PowerPlans.Add(p);
                if (p.IsActive)
                {
                    ActivePowerPlanName = p.Name;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh Gaming Optimization data", ex, "GamingViewModel");
        }
    }

    private async Task ToggleTweakAsync(OptimizationTweak? tweak)
    {
        if (tweak == null) return;

        try
        {
            IsDryRun = false;
            var targetValue = tweak.IsApplied ? tweak.DefaultValue : tweak.RecommendedValue;

            var copy = new OptimizationTweak
            {
                Id = tweak.Id,
                Name = tweak.Name,
                Description = tweak.Description,
                Category = tweak.Category,
                RegistryHive = tweak.RegistryHive,
                KeyPath = tweak.KeyPath,
                ValueName = tweak.ValueName,
                ValueType = tweak.ValueType,
                CurrentValue = tweak.CurrentValue,
                RecommendedValue = targetValue,
                RequiresRestart = tweak.RequiresRestart,
                RiskLevel = tweak.RiskLevel
            };

            ActivePlan = await _tweakService.CreateOptimizationPlanAsync(new[] { copy }, IsDryRun);
            IsPlanModalOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to prepare tweak '{tweak.Name}'", ex, "GamingViewModel");
        }
    }

    private async Task OptimizeAllGamingAsync()
    {
        var unapplied = GamingTweaks.Where(t => !t.IsApplied).ToList();
        if (unapplied.Count == 0)
        {
            _logger.LogInfo("All gaming optimizations are already active.", "GamingViewModel");
            return;
        }

        IsDryRun = false;
        ActivePlan = await _tweakService.CreateOptimizationPlanAsync(unapplied, IsDryRun);
        IsPlanModalOpen = true;
    }

    private async Task ActivatePowerPlanAsync(PowerPlanInfo? plan)
    {
        if (plan == null || plan.IsActive) return;

        try
        {
            bool success = await _tweakService.SetActivePowerPlanAsync(plan.Guid);
            if (success)
            {
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to activate gaming power plan '{plan.Name}'", ex, "GamingViewModel");
        }
    }

    private async Task ExecuteActivePlanAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressPercent = 0;
        ProgressStatus = "Applying gaming configuration...";

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
            ProgressStatus = IsDryRun ? "Dry-run simulation completed." : "Gaming optimizations applied successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError("Execution error during gaming plan", ex, "GamingViewModel");
            ProgressStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
