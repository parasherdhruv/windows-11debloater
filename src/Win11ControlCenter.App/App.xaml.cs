using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Win11ControlCenter.App.ViewModels;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Infrastructure.Services;
using Win11ControlCenter.SystemEngine.Services;

namespace Win11ControlCenter.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure & Core Services
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<BackupRepository>();
        services.AddSingleton<SettingsStorage>();
        services.AddSingleton<IOperationExecutor, OperationExecutor>();
        services.AddSingleton<ISystemDiscoveryService, SystemDiscoveryService>();
        services.AddSingleton<IPackageManagerService, PackageManagerService>();
        services.AddSingleton<IServiceManagerService, ServiceManagerService>();
        services.AddSingleton<IStartupManagerService, StartupManagerService>();
        services.AddSingleton<IPrivacyService, PrivacyService>();
        services.AddSingleton<ITweakEngineService, TweakEngineService>();
        services.AddSingleton<IOptimizationProfileService, OptimizationProfileService>();
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<IRestoreService, RestoreService>();
        services.AddSingleton<IWingetService, WingetService>();
        services.AddSingleton<IThemeService, Services.ThemeService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AppDebloaterViewModel>();
        services.AddSingleton<SoftwareCenterViewModel>();
        services.AddSingleton<StartupManagerViewModel>();
        services.AddSingleton<ServicesManagerViewModel>();
        services.AddSingleton<PrivacyViewModel>();
        services.AddSingleton<OptimizationViewModel>();
        services.AddSingleton<GamingViewModel>();
        services.AddSingleton<SystemCleanerViewModel>();
        services.AddSingleton<CustomTweaksViewModel>();
        services.AddSingleton<RestoreCenterViewModel>();
        services.AddSingleton<ActivityLogsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            LogFatalException(args.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            LogFatalException(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LogFatalException(args.Exception, "DispatcherUnhandledException");
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}\n\nThe details have been logged to the activity log.",
                "Win11 Control Center - Notification",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var settingsStorage = _serviceProvider.GetRequiredService<SettingsStorage>();
        var savedTheme = settingsStorage.Load().Theme;
        themeService.ApplyTheme(string.IsNullOrWhiteSpace(savedTheme) ? "System" : savedTheme);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();

        await mainViewModel.InitializeAsync();
    }

    private static void LogFatalException(Exception? ex, string source)
    {
        if (ex == null) return;
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(localAppData, "Win11ControlCenter", "Logs");
            Directory.CreateDirectory(logDir);
            var crashPath = Path.Combine(logDir, "crash.log");
            File.AppendAllText(crashPath, $"[{DateTime.UtcNow:O}] [{source}] {ex}\n\n");
        }
        catch
        {
            // Best effort logging
        }
    }
}
