using System.Text.Json;

namespace Win11ControlCenter.Infrastructure.Services;

public class AppSettings
{
    public string Theme { get; set; } = "Dark"; // Dark, Light
    public bool AutoScanOnStartup { get; set; } = true;
    public bool AdvancedMode { get; set; } = false;
    public bool CreateRestorePointBeforeExecution { get; set; } = true;
    public int LogRetentionDays { get; set; } = 30;
    public string BackupLocation { get; set; } = string.Empty;
}

public class SettingsStorage
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsStorage()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Win11ControlCenter");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var content = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(content);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Default on failure
        }

        var defaultSettings = new AppSettings();
        Save(defaultSettings);
        return defaultSettings;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var content = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, content);
        }
        catch
        {
            // Suppress secondary IO exception
        }
    }
}

