using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Infrastructure.Services;

namespace Win11ControlCenter.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsStorage _storage;
    private readonly ILoggingService _logger;
    private readonly IThemeService _themeService;

    private AppSettings _settings;
    private string _selectedTheme = "System";
    private string _saveStatus = string.Empty;

    public AppSettings Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "System",
        "Dark",
        "Light",
        "OLED",
        "Cobalt"
    };

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                Settings.Theme = value;
                _themeService.ApplyTheme(value);
            }
        }
    }

    public string SaveStatus
    {
        get => _saveStatus;
        set => SetProperty(ref _saveStatus, value);
    }

    public string AppVersionText => "Win11 Control Center v1.2.0";
    public string DotNetVersionText => ".NET 10.0 LTS (Native Ahead-Of-Time Ready)";
    public string ArchitectureText => "Windows 11 (win-x64) Fluent Shell";

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand ResetDefaultsCommand { get; }

    public SettingsViewModel(SettingsStorage storage, ILoggingService logger, IThemeService themeService)
    {
        _storage = storage;
        _logger = logger;
        _themeService = themeService;
        _settings = _storage.Load();

        var initialTheme = !string.IsNullOrWhiteSpace(_themeService.CurrentTheme) 
            ? _themeService.CurrentTheme 
            : (string.IsNullOrWhiteSpace(_settings.Theme) ? "System" : _settings.Theme);

        _selectedTheme = initialTheme;
        _settings.Theme = initialTheme;

        SaveCommand = new RelayCommand(SaveSettings);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);

        _themeService.ThemeChanged += (s, theme) =>
        {
            if (!string.IsNullOrWhiteSpace(theme) && _selectedTheme != theme)
            {
                _selectedTheme = theme;
                _settings.Theme = theme;
                OnPropertyChanged(nameof(SelectedTheme));
            }
        };
    }

    public override Task InitializeAsync()
    {
        try
        {
            Settings = _storage.Load();
        }
        catch { }

        // Synchronize with the currently active theme from ThemeService without overwriting it
        var activeTheme = _themeService.CurrentTheme;
        if (!string.IsNullOrWhiteSpace(activeTheme))
        {
            _selectedTheme = activeTheme;
            Settings.Theme = activeTheme;
            OnPropertyChanged(nameof(SelectedTheme));
        }

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
        SelectedTheme = "System";
        SaveSettings();
    }
}
