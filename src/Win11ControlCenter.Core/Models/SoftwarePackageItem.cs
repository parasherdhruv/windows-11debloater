using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Win11ControlCenter.Core.Models;

public class SoftwarePackageItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isInstalled;
    private string _installStatus = "Not Installed";
    private string _statusMessage = string.Empty;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Category { get; set; } = "Utilities";
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "📦";
    public string HomepageUrl { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (_isInstalled != value)
            {
                _isInstalled = value;
                OnPropertyChanged();
            }
        }
    }

    public string InstallStatus
    {
        get => _installStatus;
        set
        {
            if (_installStatus != value)
            {
                _installStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

