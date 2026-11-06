using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace Win11ControlCenter.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    private string _currentPageTitle = "Dashboard";

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        set => SetProperty(ref _currentPageTitle, value);
    }

    public bool IsElevated { get; }
    public string ElevationStatusText => IsElevated ? "Administrator (Elevated)" : "Standard User (Safe)";

    // 13 ViewModels
    public DashboardViewModel DashboardVM { get; }
    public AppDebloaterViewModel AppDebloaterVM { get; }
    public SoftwareCenterViewModel SoftwareCenterVM { get; }
    public StartupManagerViewModel StartupManagerVM { get; }
    public ServicesManagerViewModel ServicesManagerVM { get; }
    public PrivacyViewModel PrivacyVM { get; }
    public OptimizationViewModel OptimizationVM { get; }
    public GamingViewModel GamingVM { get; }
    public SystemCleanerViewModel SystemCleanerVM { get; }
    public CustomTweaksViewModel CustomTweaksVM { get; }
    public RestoreCenterViewModel RestoreCenterVM { get; }
    public ActivityLogsViewModel ActivityLogsVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public IRelayCommand<string> NavigateCommand { get; }
    public IRelayCommand RelaunchElevatedCommand { get; }

    public MainViewModel(
        DashboardViewModel dashboardVM,
        AppDebloaterViewModel appDebloaterVM,
        SoftwareCenterViewModel softwareCenterVM,
        StartupManagerViewModel startupManagerVM,
        ServicesManagerViewModel servicesManagerVM,
        PrivacyViewModel privacyVM,
        OptimizationViewModel optimizationVM,
        GamingViewModel gamingVM,
        SystemCleanerViewModel systemCleanerVM,
        CustomTweaksViewModel customTweaksVM,
        RestoreCenterViewModel restoreCenterVM,
        ActivityLogsViewModel activityLogsVM,
        SettingsViewModel settingsVM)
    {
        DashboardVM = dashboardVM;
        AppDebloaterVM = appDebloaterVM;
        SoftwareCenterVM = softwareCenterVM;
        StartupManagerVM = startupManagerVM;
        ServicesManagerVM = servicesManagerVM;
        PrivacyVM = privacyVM;
        OptimizationVM = optimizationVM;
        GamingVM = gamingVM;
        SystemCleanerVM = systemCleanerVM;
        CustomTweaksVM = customTweaksVM;
        RestoreCenterVM = restoreCenterVM;
        ActivityLogsVM = activityLogsVM;
        SettingsVM = settingsVM;

        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            IsElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        _currentViewModel = DashboardVM;
        NavigateCommand = new RelayCommand<string>(NavigateToPage);
        RelaunchElevatedCommand = new RelayCommand(RelaunchElevated);
    }

    public override async Task InitializeAsync()
    {
        await DashboardVM.InitializeAsync();
    }

    private void RelaunchElevated()
    {
        try
        {
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                Application.Current.Shutdown();
            }
        }
        catch
        {
            // User cancelled UAC prompt
        }
    }

    private async void NavigateToPage(string? pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey)) return;

        ViewModelBase target = pageKey switch
        {
            "Dashboard" => DashboardVM,
            "AppDebloater" => AppDebloaterVM,
            "SoftwareCenter" => SoftwareCenterVM,
            "StartupManager" => StartupManagerVM,
            "ServicesManager" => ServicesManagerVM,
            "Privacy" => PrivacyVM,
            "Optimization" => OptimizationVM,
            "Gaming" => GamingVM,
            "SystemCleaner" => SystemCleanerVM,
            "CustomTweaks" => CustomTweaksVM,
            "RestoreCenter" => RestoreCenterVM,
            "ActivityLogs" => ActivityLogsVM,
            "Settings" => SettingsVM,
            _ => DashboardVM
        };

        CurrentViewModel = target;
        CurrentPageTitle = pageKey switch
        {
            "Dashboard" => "Dashboard",
            "AppDebloater" => "App Debloater",
            "SoftwareCenter" => "Software Center",
            "StartupManager" => "Startup Manager",
            "ServicesManager" => "Windows Services",
            "Privacy" => "Privacy & Telemetry",
            "Optimization" => "Windows Optimization",
            "Gaming" => "Gaming Optimization",
            "SystemCleaner" => "System Cleaner",
            "CustomTweaks" => "Custom Tweaks",
            "RestoreCenter" => "Restore Center",
            "ActivityLogs" => "Activity Logs",
            "Settings" => "Settings",
            _ => "Dashboard"
        };

        try
        {
            await target.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing page {pageKey}: {ex}");
        }
    }
}
