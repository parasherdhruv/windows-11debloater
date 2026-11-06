using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Infrastructure.Services;

namespace Win11ControlCenter.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStorage _storage;
    private readonly ILoggingService _logger;

    private AppSettings _settings;
    private string _selectedTheme = "Dark Theme";
    private string _saveStatus = string.Empty;

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "Dark Theme",
        "Light Theme"
    };

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                Settings.Theme = value.StartsWith("Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
                ApplyTheme(Settings.Theme);
            }
        }
    }

    public string SaveStatus
    {
        get => _saveStatus;
        set => SetProperty(ref _saveStatus, value);
    }

    public string AppVersionText => "Win11 Control Center v1.0.0";
    public string DotNetVersionText => ".NET 10.0 LTS (Native Ahead-Of-Time Ready)";
    public string ArchitectureText => "Windows 11 (win-x64) Protected Shell";

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetDefaultsCommand { get; }

    public SettingsViewModel(SettingsStorage storage, ILoggingService logger)
    {
        _storage = storage;
        _logger = logger;
        _settings = _storage.Load();

        _selectedTheme = _settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Light Theme" : "Dark Theme";

        SaveCommand = new RelayCommand(SaveSettings);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    public override Task InitializeAsync()
    {
        ApplyTheme(Settings.Theme);
        return Task.CompletedTask;
    }

    private void SaveSettings()
    {
        try
        {
            _storage.Save(Settings);
            SaveStatus = "Preferences saved successfully.";
            _logger.LogInfo("Application settings saved.", "Settings");
        }
        catch (Exception ex)
        {
            SaveStatus = $"Error: {ex.Message}";
            _logger.LogError("Failed to save settings", ex, "Settings");
        }
    }

    private void ResetDefaults()
    {
        Settings = new AppSettings();
        SelectedTheme = "Dark Theme";
        SaveSettings();
    }

    public static void ApplyTheme(string theme)
    {
        if (Application.Current == null) return;

        bool isLight = theme.Equals("Light", StringComparison.OrdinalIgnoreCase);

        if (isLight)
        {
            Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xEB));
            Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            Application.Current.Resources["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
            Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11));
            Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            Application.Current.Resources["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xE5));
        }
        else
        {
            Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
            Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18));
            Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B));
            Application.Current.Resources["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x32, 0x32, 0x32));
            Application.Current.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
            Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            Application.Current.Resources["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
        }
    }
}
