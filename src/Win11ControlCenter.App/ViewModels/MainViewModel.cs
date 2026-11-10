using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;

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

    public IThemeService ThemeService { get; }
    public string CurrentThemeMode => ThemeService.CurrentTheme;
    public string EffectiveThemeText => ThemeService.EffectiveThemeDisplayName;
    public bool IsSystemTheme => ThemeService.CurrentTheme.Equals("System", StringComparison.OrdinalIgnoreCase);
    public bool IsDarkTheme => ThemeService.CurrentTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
    public bool IsLightTheme => ThemeService.CurrentTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
    public bool IsOledTheme => ThemeService.CurrentTheme.Equals("OLED", StringComparison.OrdinalIgnoreCase);
    public bool IsCobaltTheme => ThemeService.CurrentTheme.Equals("Cobalt", StringComparison.OrdinalIgnoreCase);

    public IRelayCommand<string> SelectThemeCommand { get; }
    public IRelayCommand CycleThemeCommand { get; }

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
        SettingsViewModel settingsVM,
        IThemeService themeService)
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
        ThemeService = themeService;

        ThemeService.ThemeChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CurrentThemeMode));
            OnPropertyChanged(nameof(EffectiveThemeText));
            OnPropertyChanged(nameof(IsSystemTheme));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
            OnPropertyChanged(nameof(IsOledTheme));
            OnPropertyChanged(nameof(IsCobaltTheme));
        };

        SelectThemeCommand = new RelayCommand<string>(theme =>
        {
            if (!string.IsNullOrWhiteSpace(theme))
            {
                ThemeService.ApplyTheme(theme);
            }
        });

        CycleThemeCommand = new RelayCommand(() =>
        {
            var themes = ThemeService.AvailableThemes.ToList();
            int currentIndex = themes.IndexOf(ThemeService.CurrentTheme);
            int nextIndex = currentIndex >= 0 ? (currentIndex + 1) % themes.Count : 0;
            ThemeService.ApplyTheme(themes[nextIndex]);
        });

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
