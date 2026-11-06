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

    private static void ConfigureServices(IServiceCollection services)
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

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AppDebloaterViewModel>();
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

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();

        await mainViewModel.InitializeAsync();
    }
}
