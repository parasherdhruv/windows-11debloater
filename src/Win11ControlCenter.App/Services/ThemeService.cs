using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Infrastructure.Services;

namespace Win11ControlCenter.App.Services;

public class ThemeService : IThemeService
{
    private readonly SettingsStorage _settingsStorage;
    private readonly ILoggingService _logger;
    private string _currentTheme = "System";

    public string CurrentTheme
    {
        get => _currentTheme;
        private set => _currentTheme = value;
    }

    public string EffectiveThemeDisplayName => CurrentTheme switch
    {
        "System" => DetectWindowsIsLightTheme() ? "System (Light)" : "System (Dark)",
        "Dark" => "Fluent Dark",
        "Light" => "Fluent Light",
        "OLED" => "Midnight OLED",
        "Cobalt" => "Cyber Cobalt",
        _ => "Fluent Dark"
    };

    public bool IsDark
    {
        get
        {
            if (CurrentTheme.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return !DetectWindowsIsLightTheme();
            }
            return !CurrentTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        }
    }

    public IReadOnlyList<string> AvailableThemes { get; } = new List<string>
    {
        "System",
        "Dark",
        "Light",
        "OLED",
        "Cobalt"
    };

    public event EventHandler<string>? ThemeChanged;

    public ThemeService(SettingsStorage settingsStorage, ILoggingService logger)
    {
        _settingsStorage = settingsStorage;
        _logger = logger;

        try
        {
            var saved = _settingsStorage.Load();
            _currentTheme = string.IsNullOrWhiteSpace(saved.Theme) ? "System" : saved.Theme;
        }
        catch
        {
            _currentTheme = "System";
        }
    }

    public bool DetectWindowsIsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var val = key.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to query Windows theme personalize registry: {ex.Message}", "ThemeService");
        }

        // Windows 11 default is dark
        return false;
    }

    public void ApplyTheme(string themeName)
    {
        if (Application.Current == null) return;

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            try
            {
                if (Application.Current.Dispatcher.HasShutdownStarted || !Application.Current.Dispatcher.Thread.IsAlive)
                {
                    ApplyThemeInternal(themeName);
                    return;
                }

                var task = Application.Current.Dispatcher.InvokeAsync(() => ApplyTheme(themeName));
                if (!task.Task.Wait(TimeSpan.FromSeconds(2)))
                {
                    // Fallback to direct apply if dispatcher pump is idle/deadlocked (e.g. during headless test runs)
                    ApplyThemeInternal(themeName);
                }
                return;
            }
            catch
            {
                ApplyThemeInternal(themeName);
                return;
            }
        }

        ApplyThemeInternal(themeName);
    }

    private void ApplyThemeInternal(string themeName)
    {
        string normalized = themeName?.Trim() ?? "System";
        _currentTheme = normalized;

        string target = normalized.ToLowerInvariant() switch
        {
            "system" or "os" or "system default" => DetectWindowsIsLightTheme() ? "light" : "dark",
            "light" or "light theme" => "light",
            "oled" or "midnight" or "midnight oled" => "oled",
            "cobalt" or "cyber cobalt" => "cobalt",
            _ => "dark"
        };

        SetPaletteResources(target);

        // Save selection
        try
        {
            var settings = _settingsStorage.Load();
            settings.Theme = normalized;
            _settingsStorage.Save(settings);
        }
        catch
        {
            // Best effort persistence
        }

        _logger.LogInfo($"Theme switched to '{normalized}' (effective: {target})", "ThemeService");
        ThemeChanged?.Invoke(this, normalized);
    }

    private static void SetPaletteResources(string target)
    {
        var res = Application.Current.Resources;

        switch (target)
        {
            case "light":
                // Clean Windows 11 Fluent Light Palette
                res["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF5));
                res["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xEA, 0xEA, 0xF0));
                res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                res["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xFA));
                res["CardActiveBrush"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xF4));
                res["CardElevatedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                res["SubCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xFA));
                res["SubCardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xF4));
                res["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                res["PillRailBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xEB));
                res["ChipBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE1, 0xED, 0xF8));
                res["BannerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xED, 0xF5, 0xFD));
                res["BannerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xB8, 0xD7, 0xF7));
                res["SuccessBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
                res["SuccessBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC));
                res["WarningBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
                res["WarningBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xFC, 0xD3, 0x4D));
                res["DangerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2));
                res["DangerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));
                res["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xE2));
                res["BorderHighlightBrush"] = new SolidColorBrush(Color.FromRgb(0xBC, 0xBC, 0xCA));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xB8));
                res["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x5D, 0xA6));
                res["AccentLightBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69));
                res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(0x8B, 0x95, 0xA5));
                res["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE4, 0xEC));
                res["NavHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x10, 0x00, 0x00, 0x00));
                res["NavSelectedBrush"] = new SolidColorBrush(Color.FromArgb(0x18, 0x00, 0x67, 0xB8));
                res["NavSelectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xB8));
                res["NavIndicatorBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x67, 0xB8));
                res["ScrollBarTrackBrush"] = Brushes.Transparent;
                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromArgb(0x35, 0x00, 0x00, 0x00));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0x00, 0x00));
                res["ScrollBarThumbActiveBrush"] = new SolidColorBrush(Color.FromArgb(0x90, 0x00, 0x00, 0x00));
                break;

            case "oled":
                // Pure Pitch Black OLED Palette
                res["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                res["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x05, 0x05, 0x08));
                res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0C, 0x0C, 0x10));
                res["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x1A));
                res["CardActiveBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x24));
                res["CardElevatedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x1A));
                res["SubCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x08, 0x08, 0x0C));
                res["SubCardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x18));
                res["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x08, 0x08, 0x0C));
                res["PillRailBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0E));
                res["ChipBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0E, 0x1C, 0x28));
                res["BannerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x06, 0x12, 0x20));
                res["BannerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x13, 0x32, 0x56));
                res["SuccessBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x08, 0x1A, 0x0E));
                res["SuccessBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x3C));
                res["WarningBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x16, 0x08));
                res["WarningBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6E, 0x4D, 0x15));
                res["DangerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2A, 0x0A, 0x0E));
                res["DangerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0x27));
                res["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x28));
                res["BorderHighlightBrush"] = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x45));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                res["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x0E, 0xA5, 0xE9));
                res["AccentLightBrush"] = new SolidColorBrush(Color.FromRgb(0x7D, 0xD3, 0xFC));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xA1, 0xA1, 0xAA));
                res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(0x65, 0x65, 0x72));
                res["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x22));
                res["NavHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF));
                res["NavSelectedBrush"] = new SolidColorBrush(Color.FromArgb(0x28, 0x38, 0xBD, 0xF8));
                res["NavSelectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                res["NavIndicatorBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                res["ScrollBarTrackBrush"] = Brushes.Transparent;
                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF));
                res["ScrollBarThumbActiveBrush"] = new SolidColorBrush(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF));
                break;

            case "cobalt":
                // Cyber Cobalt Deep Navy Palette
                res["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0A, 0x0F, 0x1D));
                res["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x07, 0x0A, 0x14));
                res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
                res["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
                res["CardActiveBrush"] = new SolidColorBrush(Color.FromRgb(0x26, 0x35, 0x4D));
                res["CardElevatedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x16, 0x20, 0x32));
                res["SubCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0E, 0x14, 0x22));
                res["SubCardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x16, 0x20, 0x32));
                res["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0E, 0x14, 0x22));
                res["PillRailBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0C, 0x12, 0x20));
                res["ChipBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x14, 0x28, 0x48));
                res["BannerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0C, 0x1A, 0x30));
                res["BannerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0x70));
                res["SuccessBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0C, 0x24, 0x16));
                res["SuccessBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x3C));
                res["WarningBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2A, 0x1E, 0x0E));
                res["WarningBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6E, 0x4D, 0x15));
                res["DangerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x35, 0x10, 0x16));
                res["DangerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0x27));
                res["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x2D, 0x4A));
                res["BorderHighlightBrush"] = new SolidColorBrush(Color.FromRgb(0x2F, 0x45, 0x70));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
                res["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                res["AccentLightBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                res["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x17, 0x23, 0x3D));
                res["NavHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x15, 0x60, 0xA5, 0xFA));
                res["NavSelectedBrush"] = new SolidColorBrush(Color.FromArgb(0x28, 0x3B, 0x82, 0xF6));
                res["NavSelectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                res["NavIndicatorBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
                res["ScrollBarTrackBrush"] = Brushes.Transparent;
                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromArgb(0x40, 0x60, 0xA5, 0xFA));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x80, 0x60, 0xA5, 0xFA));
                res["ScrollBarThumbActiveBrush"] = new SolidColorBrush(Color.FromArgb(0xC0, 0x60, 0xA5, 0xFA));
                break;

            default: // "dark"
                // Fluent Dark Palette
                res["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x16));
                res["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x10, 0x10, 0x12));
                res["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x20));
                res["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x2B));
                res["CardActiveBrush"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x34));
                res["CardElevatedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x28));
                res["SubCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x19));
                res["SubCardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x28));
                res["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x19));
                res["PillRailBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1C));
                res["ChipBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x2E, 0x3D));
                res["BannerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x12, 0x1A, 0x28));
                res["BannerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x36, 0x52));
                res["SuccessBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x11, 0x29, 0x1A));
                res["SuccessBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x3C));
                res["WarningBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2E, 0x22, 0x10));
                res["WarningBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6E, 0x4D, 0x15));
                res["DangerBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x3F, 0x14, 0x18));
                res["DangerBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0x27));
                res["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x33));
                res["BorderHighlightBrush"] = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x4C));
                res["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                res["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5));
                res["AccentLightBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xCD, 0xFF));
                res["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF5));
                res["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xA1, 0xA1, 0xAA));
                res["TextMutedBrush"] = new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x7A));
                res["BadgeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x27, 0x27, 0x2A));
                res["NavHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
                res["NavSelectedBrush"] = new SolidColorBrush(Color.FromArgb(0x1E, 0xFF, 0xFF, 0xFF));
                res["NavSelectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                res["NavIndicatorBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xCD, 0xFF));
                res["ScrollBarTrackBrush"] = Brushes.Transparent;
                res["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
                res["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromArgb(0x75, 0xFF, 0xFF, 0xFF));
                res["ScrollBarThumbActiveBrush"] = new SolidColorBrush(Color.FromArgb(0xA5, 0xFF, 0xFF, 0xFF));
                break;
        }
    }
}

