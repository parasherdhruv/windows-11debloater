using Microsoft.Win32;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class PrivacyService : IPrivacyService
{
    private readonly ILoggingService _logger;
    private readonly IOperationExecutor _operationExecutor;

    public PrivacyService(ILoggingService logger, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _operationExecutor = operationExecutor;
    }

    public Task<IReadOnlyList<PrivacyTweak>> GetPrivacyTweaksAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var tweaks = GetSupportedPrivacyCatalog();

            foreach (var tweak in tweaks)
            {
                var current = RegistryOperationHandler.ReadCurrentValue($"{tweak.RegistryHive}\\{tweak.KeyPath}", tweak.ValueName);
                tweak.CurrentValue = current;
                tweak.OriginalValue = current;

                if (current != null && tweak.RecommendedValue != null)
                {
                    tweak.IsApplied = current.ToString() == tweak.RecommendedValue.ToString();
                }
                else
                {
                    tweak.IsApplied = false;
                }
            }

            _logger.LogInfo($"Privacy settings scan: {tweaks.Count} documented settings checked.", "PrivacyService");
            return (IReadOnlyList<PrivacyTweak>)tweaks;
        }, cancellationToken);
    }

    public async Task<OperationPlan> CreatePrivacyPlanAsync(IEnumerable<PrivacyTweak> tweaks, bool isDryRun)
    {
        var requestedItems = new List<OperationItem>();

        foreach (var tweak in tweaks)
        {
            var item = new OperationItem
            {
                Name = tweak.Name,
                Description = tweak.Description,
                Category = $"Privacy: {tweak.Category}",
                RiskLevel = tweak.RiskLevel,
                OperationType = OperationType.Registry,
                Reversibility = ReversibilityTier.FullyReversible,
                Target = $"{tweak.RegistryHive}\\{tweak.KeyPath}",
                OriginalValue = tweak.CurrentValue,
                RecommendedValue = tweak.RecommendedValue,
                ValueType = tweak.ValueType,
                RequiresAdmin = tweak.RequiresAdmin,
                RequiresRestart = tweak.RequiresRestart,
                RollbackSupported = true,
                Metadata = new Dictionary<string, string>
                {
                    { "TweakId", tweak.Id },
                    { "ValueName", tweak.ValueName },
                    { "Category", tweak.Category }
                }
            };

            requestedItems.Add(item);
        }

        return await _operationExecutor.GeneratePlanAsync(requestedItems, "Privacy Baseline", isDryRun);
    }

    public async Task<OperationPlan> CreateRevertPlanAsync(IEnumerable<PrivacyTweak> tweaks, bool isDryRun)
    {
        var requestedItems = new List<OperationItem>();

        foreach (var tweak in tweaks)
        {
            var targetValue = tweak.DefaultValue;

            var item = new OperationItem
            {
                Name = $"Revert: {tweak.Name}",
                Description = $"Restores default Windows setting for {tweak.Name}.",
                Category = $"Privacy Revert: {tweak.Category}",
                RiskLevel = OperationRiskLevel.Safe,
                OperationType = OperationType.Registry,
                Reversibility = ReversibilityTier.FullyReversible,
                Target = $"{tweak.RegistryHive}\\{tweak.KeyPath}",
                OriginalValue = tweak.CurrentValue,
                RecommendedValue = targetValue,
                ValueType = tweak.ValueType,
                RequiresAdmin = tweak.RequiresAdmin,
                RequiresRestart = tweak.RequiresRestart,
                RollbackSupported = true,
                Metadata = new Dictionary<string, string>
                {
                    { "TweakId", tweak.Id },
                    { "ValueName", tweak.ValueName },
                    { "Category", tweak.Category }
                }
            };

            requestedItems.Add(item);
        }

        return await _operationExecutor.GeneratePlanAsync(requestedItems, "Revert Privacy Settings", isDryRun);
    }

    public static List<PrivacyTweak> GetSupportedPrivacyCatalog()
    {
        return new List<PrivacyTweak>
        {
            new()
            {
                Id = "disable-advertising-id",
                Name = "Disable Advertising ID",
                Description = "Prevents Windows and installed applications from tracking your app usage to serve personalized ads.",
                Category = "Advertising",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                ValueName = "Enabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-tailored-experiences",
                Name = "Disable Tailored Experiences",
                Description = "Stops Microsoft from analyzing diagnostic data to show personalized recommendations and promotions.",
                Category = "Telemetry",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Privacy",
                ValueName = "TailoredExperiencesWithDiagnosticDataEnabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "minimize-telemetry-data-collection",
                Name = "Limit Diagnostic Data to Required Only",
                Description = "Restricts Windows Diagnostic Data collection to the minimum required system health baseline.",
                Category = "Telemetry",
                RegistryHive = "HKLM",
                KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                ValueName = "AllowTelemetry",
                ValueType = "DWord",
                RecommendedValue = 1, // 1 = Required Diagnostic Data (Standard documented Group Policy level)
                DefaultValue = 3, // Full telemetry
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-activity-history-feed",
                Name = "Disable Activity History Feed",
                Description = "Prevents Windows from storing user activities and timeline history.",
                Category = "Activity History",
                RegistryHive = "HKLM",
                KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
                ValueName = "EnableActivityFeed",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-activity-history-publish",
                Name = "Disable User Activity Cloud Sync",
                Description = "Stops Windows from uploading user activity history to Microsoft cloud services.",
                Category = "Activity History",
                RegistryHive = "HKLM",
                KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\System",
                ValueName = "PublishUserActivities",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-feedback-frequency",
                Name = "Disable Windows Feedback Surveys",
                Description = "Sets Windows feedback prompt frequency to Never.",
                Category = "Feedback",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Siuf\Rules",
                ValueName = "NumberOfSIUFInPeriod",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-start-bing-search",
                Name = "Disable Bing Web Search in Start Menu",
                Description = "Limits Start menu searching to local files, settings, and apps without querying Bing online.",
                Category = "Search Privacy",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Search",
                ValueName = "BingSearchEnabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-app-launch-tracking",
                Name = "Disable App Launch Tracking",
                Description = "Stops Windows from tracking app launches to improve Start and search results.",
                Category = "Tracking",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "Start_TrackProgs",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-suggested-content-settings",
                Name = "Disable Suggested Content in Settings App",
                Description = "Prevents promotional and suggested content from appearing in the Windows Settings app.",
                Category = "Suggestions",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                ValueName = "SubscribedContent-338393Enabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-start-suggestions",
                Name = "Disable Start Menu App Recommendations",
                Description = "Disables promoted and suggested app recommendations in the Start Menu.",
                Category = "Suggestions",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                ValueName = "SystemPaneSuggestionsEnabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            }
        };
    }
}
