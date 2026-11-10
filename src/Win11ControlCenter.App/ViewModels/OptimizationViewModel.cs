using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class OptimizationViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly ITweakEngineService _tweakService;
    private readonly IOptimizationProfileService _profileService;
    private readonly IOperationExecutor _operationExecutor;

    private OptimizationProfile? _selectedProfile;
    private int _selectedTabIndex;

    private bool _isPlanModalOpen;
    private bool _isDiffModalOpen;
    private bool _isImportModalOpen;
    private OperationPlan? _activePlan;
    private ProfileDiffResult? _currentDiff;
    private PortableMachineProfile? _importedProfile;
    private string _statusNotification = string.Empty;
    private double _progressPercent;
    private string _progressStatus = string.Empty;
    private bool _isExecuting;
    private bool _isDryRun;

    public ObservableCollection<OptimizationProfile> Profiles { get; } = new();
    public ObservableCollection<OptimizationTweak> ExplorerTweaks { get; } = new();
    public ObservableCollection<PowerPlanInfo> PowerPlans { get; } = new();
    public ObservableCollection<ProfileDiffItem> DiffItems { get; } = new();

    public OptimizationProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public bool IsPlanModalOpen
    {
        get => _isPlanModalOpen;
        set => SetProperty(ref _isPlanModalOpen, value);
    }

    public bool IsDiffModalOpen
    {
        get => _isDiffModalOpen;
        set => SetProperty(ref _isDiffModalOpen, value);
    }

    public bool IsImportModalOpen
    {
        get => _isImportModalOpen;
        set => SetProperty(ref _isImportModalOpen, value);
    }

    public OperationPlan? ActivePlan
    {
        get => _activePlan;
        set => SetProperty(ref _activePlan, value);
    }

    public ProfileDiffResult? CurrentDiff
    {
        get => _currentDiff;
        set => SetProperty(ref _currentDiff, value);
    }

    public PortableMachineProfile? ImportedProfile
    {
        get => _importedProfile;
        set => SetProperty(ref _importedProfile, value);
    }

    public string StatusNotification
    {
        get => _statusNotification;
        set => SetProperty(ref _statusNotification, value);
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
    public IAsyncRelayCommand InspectDiffCommand { get; }
    public IAsyncRelayCommand ApplyProfileCommand { get; }
    public IAsyncRelayCommand<OptimizationTweak> ToggleExplorerTweakCommand { get; }
    public IAsyncRelayCommand ApplyAllExplorerTweaksCommand { get; }
    public IAsyncRelayCommand<PowerPlanInfo> ActivatePowerPlanCommand { get; }
    public IAsyncRelayCommand ExecutePlanCommand { get; }
    public IRelayCommand CancelPlanCommand { get; }
    public IRelayCommand CloseDiffModalCommand { get; }

    public IAsyncRelayCommand ImportProfileCommand { get; }
    public IAsyncRelayCommand ExportProfileCommand { get; }
    public IAsyncRelayCommand ExportCurrentSystemCommand { get; }
    public IAsyncRelayCommand ApplyImportedProfileCommand { get; }
    public IAsyncRelayCommand SaveImportedAsCustomCommand { get; }
    public IRelayCommand CloseImportModalCommand { get; }

    public OptimizationViewModel(
        ILoggingService logger,
        ITweakEngineService tweakService,
        IOptimizationProfileService profileService,
        IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _tweakService = tweakService;
        _profileService = profileService;
        _operationExecutor = operationExecutor;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        InspectDiffCommand = new AsyncRelayCommand(InspectDiffAsync);
        ApplyProfileCommand = new AsyncRelayCommand(ApplyProfileAsync);
        ToggleExplorerTweakCommand = new AsyncRelayCommand<OptimizationTweak>(ToggleExplorerTweakAsync);
        ApplyAllExplorerTweaksCommand = new AsyncRelayCommand(ApplyAllExplorerTweaksAsync);
        ActivatePowerPlanCommand = new AsyncRelayCommand<PowerPlanInfo>(ActivatePowerPlanAsync);
        ExecutePlanCommand = new AsyncRelayCommand(ExecuteActivePlanAsync);
        CancelPlanCommand = new RelayCommand(() => IsPlanModalOpen = false);
        CloseDiffModalCommand = new RelayCommand(() => IsDiffModalOpen = false);

        ImportProfileCommand = new AsyncRelayCommand(ImportProfileAsync);
        ExportProfileCommand = new AsyncRelayCommand(ExportProfileAsync);
        ExportCurrentSystemCommand = new AsyncRelayCommand(ExportCurrentSystemAsync);
        ApplyImportedProfileCommand = new AsyncRelayCommand(ApplyImportedProfileAsync);
        SaveImportedAsCustomCommand = new AsyncRelayCommand(SaveImportedAsCustomAsync);
        CloseImportModalCommand = new RelayCommand(() => IsImportModalOpen = false);
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            // 1. Profiles
            Profiles.Clear();
            var predefined = await _profileService.GetPredefinedProfilesAsync();
            var custom = await _profileService.GetCustomProfilesAsync();

            foreach (var p in predefined.Concat(custom))
            {
                Profiles.Add(p);
            }

            if (SelectedProfile == null && Profiles.Count > 0)
            {
                SelectedProfile = Profiles[1]; // default to Balanced
            }

            // 2. Explorer Tweaks
            ExplorerTweaks.Clear();
            var tweaks = await _tweakService.GetExplorerTweaksAsync();
            foreach (var t in tweaks)
            {
                ExplorerTweaks.Add(t);
            }

            // 3. Power Plans
            PowerPlans.Clear();
            var plans = await _tweakService.GetPowerPlansAsync();
            foreach (var plan in plans)
            {
                PowerPlans.Add(plan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize Windows Optimization view", ex, "OptimizationViewModel");
        }
    }

    private async Task InspectDiffAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            CurrentDiff = await _profileService.CalculateProfileDiffAsync(SelectedProfile);
            DiffItems.Clear();
            foreach (var item in CurrentDiff.Items)
            {
                DiffItems.Add(item);
            }

            IsDiffModalOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to calculate diff for profile '{SelectedProfile.Name}'", ex, "OptimizationViewModel");
        }
    }

    private async Task ApplyProfileAsync()
    {
        if (SelectedProfile == null) return;

        try
        {
            IsDryRun = false;
            ActivePlan = await _profileService.CreateProfilePlanAsync(SelectedProfile, IsDryRun);
            IsPlanModalOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate plan for profile '{SelectedProfile.Name}'", ex, "OptimizationViewModel");
        }
    }

    private async Task ToggleExplorerTweakAsync(OptimizationTweak? tweak)
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
                RiskLevel = tweak.RiskLevel
            };

            ActivePlan = await _tweakService.CreateOptimizationPlanAsync(new[] { copy }, IsDryRun);
            IsPlanModalOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate tweak plan for '{tweak.Name}'", ex, "OptimizationViewModel");
        }
    }

    private async Task ApplyAllExplorerTweaksAsync()
    {
        var unapplied = ExplorerTweaks.Where(t => !t.IsApplied).ToList();
        if (unapplied.Count == 0)
        {
            _logger.LogInfo("All Explorer optimizations are already active.", "OptimizationViewModel");
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
            _logger.LogError($"Failed to activate power scheme '{plan.Name}'", ex, "OptimizationViewModel");
        }
    }

    private async Task ExecuteActivePlanAsync()
    {
        if (ActivePlan == null) return;

        IsExecuting = true;
        ProgressPercent = 0;
        ProgressStatus = "Starting execution...";

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
            ProgressStatus = IsDryRun ? "Dry-run simulation completed." : "Optimization plan executed successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError("Execution error during optimization plan", ex, "OptimizationViewModel");
            ProgressStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private async Task ImportProfileAsync()
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Windows 11 Optimization Profile (.w11profile)",
                Filter = "Win11 Machine Profile (*.w11profile;*.json)|*.w11profile;*.json|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                var imported = await _profileService.ImportPortableProfileFromFileAsync(dlg.FileName);
                if (imported != null)
                {
                    ImportedProfile = imported;

                    var optProfile = imported.ToOptimizationProfile();
                    CurrentDiff = await _profileService.CalculateProfileDiffAsync(optProfile);
                    DiffItems.Clear();
                    foreach (var item in CurrentDiff.Items)
                    {
                        DiffItems.Add(item);
                    }

                    IsImportModalOpen = true;
                    StatusNotification = $"Loaded profile '{imported.Name}' with {imported.TotalActionCount} actions.";
                }
                else
                {
                    StatusNotification = "Could not parse or validate the selected profile file.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error importing portable profile", ex, "OptimizationViewModel");
            StatusNotification = $"Import failed: {ex.Message}";
        }
    }

    private async Task ExportProfileAsync()
    {
        try
        {
            var source = SelectedProfile != null
                ? PortableMachineProfile.FromOptimizationProfile(SelectedProfile)
                : await _profileService.CaptureCurrentSystemProfileAsync("System Snapshot", "Custom machine configuration");

            var sanitizedName = string.Join("_", source.Name.Split(Path.GetInvalidFileNameChars()));
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Portable Optimization Profile (.w11profile)",
                Filter = "Win11 Machine Profile (*.w11profile)|*.w11profile|JSON Profile (*.json)|*.json",
                FileName = $"{sanitizedName}_{DateTime.Now:yyyyMMdd}.w11profile"
            };

            if (dlg.ShowDialog() == true)
            {
                await _profileService.ExportPortableProfileToFileAsync(source, dlg.FileName);
                StatusNotification = $"Profile exported to '{Path.GetFileName(dlg.FileName)}'.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error exporting portable profile", ex, "OptimizationViewModel");
            StatusNotification = $"Export failed: {ex.Message}";
        }
    }

    private async Task ExportCurrentSystemAsync()
    {
        try
        {
            var systemSnapshot = await _profileService.CaptureCurrentSystemProfileAsync(
                $"{Environment.MachineName} Baseline",
                $"Full configuration snapshot of {Environment.MachineName} running Windows 11.");

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Current System State Profile (.w11profile)",
                Filter = "Win11 Machine Profile (*.w11profile)|*.w11profile|JSON Profile (*.json)|*.json",
                FileName = $"{Environment.MachineName}_Snapshot_{DateTime.Now:yyyyMMdd}.w11profile"
            };

            if (dlg.ShowDialog() == true)
            {
                await _profileService.ExportPortableProfileToFileAsync(systemSnapshot, dlg.FileName);
                StatusNotification = $"System snapshot exported to '{Path.GetFileName(dlg.FileName)}'.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error capturing and exporting system profile", ex, "OptimizationViewModel");
            StatusNotification = $"Snapshot export failed: {ex.Message}";
        }
    }

    private async Task ApplyImportedProfileAsync()
    {
        if (ImportedProfile == null) return;

        try
        {
            IsDryRun = false;
            ActivePlan = await _profileService.CreatePortableProfileExecutionPlanAsync(ImportedProfile, IsDryRun);
            IsImportModalOpen = false;
            IsPlanModalOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate execution plan for imported profile '{ImportedProfile.Name}'", ex, "OptimizationViewModel");
            StatusNotification = $"Execution error: {ex.Message}";
        }
    }

    private async Task SaveImportedAsCustomAsync()
    {
        if (ImportedProfile == null) return;

        try
        {
            var optProfile = ImportedProfile.ToOptimizationProfile();
            var success = await _profileService.SaveCustomProfileAsync(optProfile);
            if (success)
            {
                await RefreshAsync();
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == optProfile.Id) ?? optProfile;
                IsImportModalOpen = false;
                StatusNotification = $"Saved '{ImportedProfile.Name}' to your custom profiles library.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save imported profile as custom: {ex.Message}", ex, "OptimizationViewModel");
        }
    }
}
