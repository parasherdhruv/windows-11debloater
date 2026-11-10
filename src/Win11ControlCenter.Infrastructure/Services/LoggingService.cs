using System.Collections.Concurrent;
using System.Text.Json;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.Infrastructure.Services;

public class LoggingService : ILoggingService
{
    private readonly string _logDirectory;
    private readonly string _auditFilePath;
    private readonly ConcurrentQueue<OperationItem> _memoryAuditHistory = new();
    private static readonly object _fileLock = new();

    public LoggingService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(localAppData, "Win11ControlCenter", "Logs");
        Directory.CreateDirectory(_logDirectory);
        _auditFilePath = Path.Combine(_logDirectory, "audit.jsonl");

        LoadRecentHistoryFromFile();
    }

    public void LogInfo(string message, string? category = null) => WriteLogLine("INFO", message, category);
    public void LogWarning(string message, string? category = null) => WriteLogLine("WARN", message, category);
    public void LogError(string message, Exception? ex = null, string? category = null)
    {
        var fullMsg = ex == null ? message : $"{message} | Exception: {ex.Message}\nStack: {ex.StackTrace}";
        WriteLogLine("ERROR", fullMsg, category);
    }

    public void LogOperation(OperationItem item)
    {
        if (item == null) return;
        _memoryAuditHistory.Enqueue(item);

        try
        {
            var line = JsonSerializer.Serialize(item);
            lock (_fileLock)
            {
                File.AppendAllLines(_auditFilePath, new[] { line });
            }
        }
        catch
        {
            // Fallback: in-memory preserved
        }
    }

    public Task<IReadOnlyList<OperationItem>> GetAuditHistoryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OperationItem>>(_memoryAuditHistory.ToList());
    }

    public Task<bool> ClearAuditHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryAuditHistory.Clear();
            lock (_fileLock)
            {
                if (File.Exists(_auditFilePath))
                {
                    File.Delete(_auditFilePath);
                }
            }
            LogInfo("Audit history cleared by user request.", "LoggingService");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            LogError("Failed to clear audit history", ex, "LoggingService");
            return Task.FromResult(false);
        }
    }

    public async Task<string> ExportAuditHistoryAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        var items = _memoryAuditHistory.ToList();
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(destinationPath, json, cancellationToken);
        LogInfo($"Audit log exported ({items.Count} items) to: {destinationPath}", "LoggingService");
        return destinationPath;
    }

    private void WriteLogLine(string level, string message, string? category)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        var logLine = $"[{timestamp}] [{level}] [{(category ?? "General")}] {message}";
        
        try
        {
            var textLogPath = Path.Combine(_logDirectory, "activity.log");
            lock (_fileLock)
            {
                File.AppendAllLines(textLogPath, new[] { logLine });
            }
        }
        catch
        {
            // Suppress secondary logging failures
        }
    }

    private void LoadRecentHistoryFromFile()
    {
        try
        {
            if (!File.Exists(_auditFilePath)) return;
            var lines = File.ReadAllLines(_auditFilePath);
            // Load last 200 items into memory
            foreach (var line in lines.TakeLast(200))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var item = JsonSerializer.Deserialize<OperationItem>(line);
                    if (item != null)
                    {
                        _memoryAuditHistory.Enqueue(item);
                    }
                }
                catch
                {
                    // Skip malformed record, continue loading valid history
                }
            }
        }
        catch
        {
            // Fresh start if corrupted
        }
    }
}

