using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Infrastructure.Services;

namespace Win11ControlCenter.App.ViewModels;

public partial class RestoreCenterViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private readonly IRestoreService _restoreService;
    private readonly BackupRepository _backupRepository;

    private BackupMetadata? _selectedBackup;
    private List<BackupMetadata> _allBackups = new();
    private string _searchFilter = string.Empty;
    private bool _isSystemRestoreAvailable;
    private string _systemRestoreStatusText = "Checking...";
    private string _restorePointDescription = "Win11ControlCenter Checkpoint";
    private bool _isRestoring;
    private double _restoreProgress;
    private string _restoreStatusMessage = string.Empty;
    private bool _isRestoreModalOpen;

    public ObservableCollection<BackupMetadata> Backups { get; } = new();

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public BackupMetadata? SelectedBackup
    {
        get => _selectedBackup;
        set => SetProperty(ref _selectedBackup, value);
    }

    public bool IsSystemRestoreAvailable
    {
        get => _isSystemRestoreAvailable;
        set => SetProperty(ref _isSystemRestoreAvailable, value);
    }

    public string SystemRestoreStatusText
    {
        get => _systemRestoreStatusText;
        set => SetProperty(ref _systemRestoreStatusText, value);
    }

    public string RestorePointDescription
    {
        get => _restorePointDescription;
        set => SetProperty(ref _restorePointDescription, value);
    }

    public bool IsRestoring
    {
        get => _isRestoring;
        set
        {
            if (SetProperty(ref _isRestoring, value))
            {
                OnPropertyChanged(nameof(IsNotRestoring));
            }
        }
    }

    public bool IsNotRestoring => !IsRestoring;

    public double RestoreProgress
    {
        get => _restoreProgress;
        set => SetProperty(ref _restoreProgress, value);
    }

    public string RestoreStatusMessage
    {
        get => _restoreStatusMessage;
        set => SetProperty(ref _restoreStatusMessage, value);
    }

    public bool IsRestoreModalOpen
    {
        get => _isRestoreModalOpen;
        set => SetProperty(ref _isRestoreModalOpen, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand CreateRestorePointCommand { get; }
    public IAsyncRelayCommand RestoreSelectedBackupCommand { get; }
    public IRelayCommand OpenBackupFolderCommand { get; }
    public IAsyncRelayCommand DeleteBackupCommand { get; }
    public IRelayCommand CloseModalCommand { get; }

    public RestoreCenterViewModel(
        ILoggingService logger,
        IRestoreService restoreService,
        BackupRepository backupRepository)
    {
        _logger = logger;
        _restoreService = restoreService;
        _backupRepository = backupRepository;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CreateRestorePointCommand = new AsyncRelayCommand(CreateRestorePointAsync);
        RestoreSelectedBackupCommand = new AsyncRelayCommand(RestoreSelectedBackupAsync);
        OpenBackupFolderCommand = new RelayCommand(OpenBackupFolder);
        DeleteBackupCommand = new AsyncRelayCommand(DeleteSelectedBackupAsync);
        CloseModalCommand = new RelayCommand(() => IsRestoreModalOpen = false);
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            IsSystemRestoreAvailable = _restoreService.IsSystemRestoreAvailable();
            SystemRestoreStatusText = IsSystemRestoreAvailable ? "Available / Enabled" : "Disabled or Restricted";

            var list = await _restoreService.GetBackupsAsync();
            _allBackups = list.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.BackupId).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh restore center", ex, "RestoreCenterViewModel");
        }
    }

    private void ApplyFilter()
    {
        var prevSelectedId = SelectedBackup?.BackupId;
        Backups.Clear();
        var query = _allBackups.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(b =>
                b.BackupId.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                b.TriggerProfile.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                b.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                b.FormattedTimestamp.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                b.ShortId.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var b in query)
        {
            Backups.Add(b);
        }

        SelectedBackup = Backups.FirstOrDefault(b => b.BackupId == prevSelectedId) ?? Backups.FirstOrDefault();
    }

    private async Task CreateRestorePointAsync()
    {
        IsRestoring = true;
        RestoreStatusMessage = "Creating Windows System Restore point...";
        IsRestoreModalOpen = true;

        try
        {
            bool ok = await _restoreService.CreateSystemRestorePointAsync(RestorePointDescription);
            RestoreStatusMessage = ok 
                ? $"System Restore checkpoint created: '{RestorePointDescription}'" 
                : "Unable to create System Restore point (requires Administrator privileges or System Protection enabled).";
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating system restore point", ex, "RestoreCenter");
            RestoreStatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRestoring = false;
        }
    }

    private async Task RestoreSelectedBackupAsync()
    {
        if (SelectedBackup == null) return;

        IsRestoring = true;
        RestoreProgress = 0;
        RestoreStatusMessage = $"Restoring configuration from backup '{SelectedBackup.BackupId}'...";
        IsRestoreModalOpen = true;

        try
        {
            bool success = await _restoreService.RestoreBackupAsync(SelectedBackup.BackupId);
            RestoreProgress = 100;
            RestoreStatusMessage = success 
                ? "Rollback complete! All pre-optimization settings have been restored." 
                : "Rollback completed with warnings. Please review activity logs.";

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed restoring backup {SelectedBackup.BackupId}", ex, "RestoreCenter");
            RestoreStatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRestoring = false;
        }
    }

    private void OpenBackupFolder()
    {
        if (SelectedBackup == null || !Directory.Exists(SelectedBackup.Path))
        {
            var root = _backupRepository.GetBackupsRoot();
            if (Directory.Exists(root))
            {
                Process.Start("explorer.exe", root);
            }
            return;
        }

        try
        {
            Process.Start("explorer.exe", SelectedBackup.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed opening backup folder in explorer", ex, "RestoreCenter");
        }
    }

    private async Task DeleteSelectedBackupAsync()
    {
        if (SelectedBackup == null) return;

        var id = SelectedBackup.BackupId;
        bool deleted = await _restoreService.DeleteBackupAsync(id);
        if (deleted)
        {
            await RefreshAsync();
        }
    }
}
