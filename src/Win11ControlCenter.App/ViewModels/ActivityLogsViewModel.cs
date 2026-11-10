using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.App.ViewModels;

public partial class ActivityLogsViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;
    private List<OperationItem> _allEntries = new();

    private string _searchFilter = string.Empty;
    private string _selectedCategory = "All";
    private string _selectedSeverity = "All";
    private string _exportStatus = string.Empty;

    public ObservableCollection<OperationItem> FilteredEntries { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<string> Severities { get; } = new();

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

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set
        {
            if (SetProperty(ref _selectedSeverity, value))
            {
                ApplyFilter();
            }
        }
    }

    public string ExportStatus
    {
        get => _exportStatus;
        set => SetProperty(ref _exportStatus, value);
    }

    public int TotalLogCount => _allEntries.Count;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand ExportLogsCommand { get; }
    public IAsyncRelayCommand ClearLogsCommand { get; }

    public ActivityLogsViewModel(ILoggingService logger)
    {
        _logger = logger;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ExportLogsCommand = new AsyncRelayCommand(ExportLogsAsync);
        ClearLogsCommand = new AsyncRelayCommand(ClearLogsAsync);

        Severities.Add("All");
        Severities.Add("Succeeded");
        Severities.Add("Failed");
        Severities.Add("Skipped");
        Severities.Add("RolledBack");

        Categories.Add("All");
    }

    public override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var history = await _logger.GetAuditHistoryAsync();
            _allEntries = history.OrderByDescending(h => h.ExecutedAt).ToList();

            var cats = _allEntries
                .Select(e => e.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var prevCategory = SelectedCategory;
            Categories.Clear();
            Categories.Add("All");
            foreach (var c in cats)
            {
                Categories.Add(c);
            }

            SelectedCategory = !string.IsNullOrWhiteSpace(prevCategory) && (prevCategory == "All" || cats.Contains(prevCategory))
                ? prevCategory
                : "All";

            ApplyFilter();
            OnPropertyChanged(nameof(TotalLogCount));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh activity logs", ex, "ActivityLogs");
        }
    }

    private void ApplyFilter()
    {
        FilteredEntries.Clear();
        var query = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
        {
            query = query.Where(e => e.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedSeverity) && SelectedSeverity != "All")
        {
            if (Enum.TryParse<ExecutionStatus>(SelectedSeverity, out var st))
            {
                query = query.Where(e => e.Status == st);
            }
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            var search = SearchFilter.Trim();
            query = query.Where(e =>
                (e.Name != null && e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.Target != null && e.Target.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.ResultMessage != null && e.ResultMessage.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var entry in query)
        {
            FilteredEntries.Add(entry);
        }
    }

    private async Task ExportLogsAsync()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var exportPath = Path.Combine(localAppData, "Win11ControlCenter", $"audit_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await _logger.ExportAuditHistoryAsync(exportPath);
            ExportStatus = $"Exported to: {Path.GetFileName(exportPath)}";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed exporting audit logs", ex, "ActivityLogs");
            ExportStatus = $"Export error: {ex.Message}";
        }
    }

    private async Task ClearLogsAsync()
    {
        try
        {
            await _logger.ClearAuditHistoryAsync();
            await RefreshAsync();
            ExportStatus = "Audit history cleared.";
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to clear audit history", ex, "ActivityLogs");
        }
    }
}
