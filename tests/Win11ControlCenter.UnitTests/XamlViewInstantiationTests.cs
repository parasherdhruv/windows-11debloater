using System.Threading;
using System.Windows;
using Win11ControlCenter.App;
using Win11ControlCenter.App.Views;
using Xunit;

namespace Win11ControlCenter.UnitTests;

public class XamlViewInstantiationTests
{
    private static void RunInSta(Action action)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new Application();
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/Win11ControlCenter.App;component/Themes/ThemeResources.xaml")
                        });
                    }
                    else if (Application.Current.Resources.MergedDictionaries.Count == 0)
                    {
                        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/Win11ControlCenter.App;component/Themes/ThemeResources.xaml")
                        });
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignore if Application was already initialized by another test
                }

                action();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (thrown != null)
        {
            throw new AggregateException("STA Execution failed", thrown);
        }
    }

    [Fact]
    public void MainWindow_InstantiatesWithoutXamlParseException()
    {
        RunInSta(() =>
        {
            var win = new MainWindow();
            Assert.NotNull(win);
        });
    }

    [Fact]
    public void AllViews_InstantiateWithoutXamlParseException()
    {
        RunInSta(() =>
        {
            Assert.NotNull(new DashboardView());
            Assert.NotNull(new AppDebloaterView());
            Assert.NotNull(new SoftwareCenterView());
            Assert.NotNull(new StartupManagerView());
            Assert.NotNull(new ServicesManagerView());
            Assert.NotNull(new PrivacyView());
            Assert.NotNull(new OptimizationView());
            Assert.NotNull(new GamingView());
            Assert.NotNull(new SystemCleanerView());
            Assert.NotNull(new CustomTweaksView());
            Assert.NotNull(new RestoreCenterView());
            Assert.NotNull(new ActivityLogsView());
            Assert.NotNull(new SettingsView());
        });
    }

    [Fact]
    public void SoftwareCenterView_WithDataContext_RendersWithoutErrors()
    {
        RunInSta(() =>
        {
            var logger = new Win11ControlCenter.Infrastructure.Services.LoggingService();
            var winget = new Win11ControlCenter.SystemEngine.Services.WingetService(logger);
            var vm = new Win11ControlCenter.App.ViewModels.SoftwareCenterViewModel(winget, logger);
            var view = new SoftwareCenterView { DataContext = vm };
            Assert.NotNull(view);
            Assert.NotNull(view.Content);
        });
    }
}
