using System.Diagnostics;
using System.Text.RegularExpressions;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.SystemEngine.Handlers;

namespace Win11ControlCenter.SystemEngine.Services;

public class TweakEngineService : ITweakEngineService
{
    private static readonly Regex GuidRegex = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
    private static readonly Regex PowerCfgLineRegex = new(@"Power Scheme GUID:\s+([0-9a-fA-F\-]+)\s+\((.*?)\)(\s+\*)?", RegexOptions.Compiled);

    private readonly ILoggingService _logger;
    private readonly IOperationExecutor _operationExecutor;

    public TweakEngineService(ILoggingService logger, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _operationExecutor = operationExecutor;
    }

    public Task<IReadOnlyList<OptimizationTweak>> GetExplorerTweaksAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var tweaks = GetSupportedExplorerCatalog();

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

            _logger.LogInfo($"Explorer tweaks scan: {tweaks.Count} settings evaluated.", "TweakEngineService");
            return (IReadOnlyList<OptimizationTweak>)tweaks;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<OptimizationTweak>> GetGamingTweaksAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var tweaks = GetSupportedGamingCatalog();

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

            _logger.LogInfo($"Gaming tweaks scan: {tweaks.Count} settings evaluated.", "TweakEngineService");
            return (IReadOnlyList<OptimizationTweak>)tweaks;
        }, cancellationToken);
    }

    public async Task<OperationPlan> CreateOptimizationPlanAsync(IEnumerable<OptimizationTweak> tweaks, bool isDryRun)
    {
        var requestedItems = new List<OperationItem>();

        foreach (var tweak in tweaks)
        {
            var item = new OperationItem
            {
                Name = tweak.Name,
                Description = tweak.Description,
                Category = $"Optimization: {tweak.Category}",
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

        return await _operationExecutor.GeneratePlanAsync(requestedItems, "System & Explorer Optimization", isDryRun);
    }

    public async Task<IReadOnlyList<PowerPlanInfo>> GetPowerPlansAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var plans = new List<PowerPlanInfo>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return plans;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var match = PowerCfgLineRegex.Match(line);
                    if (match.Success)
                    {
                        var guid = match.Groups[1].Value.Trim();
                        var name = match.Groups[2].Value.Trim();
                        var isActive = line.Contains('*');

                        plans.Add(new PowerPlanInfo
                        {
                            Guid = guid,
                            Name = name,
                            IsActive = isActive
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to enumerate Windows power schemes via powercfg", ex, "PowerPlan");
            }

            return (IReadOnlyList<PowerPlanInfo>)plans;
        }, cancellationToken);
    }

    public async Task<bool> SetActivePowerPlanAsync(string planGuid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(planGuid) || !GuidRegex.IsMatch(planGuid))
        {
            _logger.LogWarning($"Rejected power plan activation: Invalid GUID '{planGuid}'.", "PowerPlan");
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = $"/setactive {planGuid}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                process.WaitForExit(5000);
                bool success = process.ExitCode == 0;

                if (success)
                {
                    _logger.LogInfo($"Active power plan switched to GUID: {planGuid}", "PowerPlan");
                }
                else
                {
                    string err = process.StandardError.ReadToEnd();
                    _logger.LogError($"Failed to switch power plan ({planGuid}): {err}", null, "PowerPlan");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception switching power plan to {planGuid}", ex, "PowerPlan");
                return false;
            }
        }, cancellationToken);
    }

    public static List<OptimizationTweak> GetSupportedExplorerCatalog()
    {
        return new List<OptimizationTweak>
        {
            new()
            {
                Id = "show-file-extensions",
                Name = "Show File Extensions",
                Description = "Displays file extensions (.txt, .exe, etc.) in File Explorer for clearer file identification.",
                Category = "File Explorer",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "HideFileExt",
                ValueType = "DWord",
                RecommendedValue = 0, // 0 = Show
                DefaultValue = 1,     // 1 = Hide
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "show-hidden-files",
                Name = "Show Hidden Files",
                Description = "Reveals hidden files and folders in File Explorer.",
                Category = "File Explorer",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "Hidden",
                ValueType = "DWord",
                RecommendedValue = 1, // 1 = Show
                DefaultValue = 2,     // 2 = Hide
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "compact-view-explorer",
                Name = "Compact View in File Explorer",
                Description = "Reduces vertical padding between list items in File Explorer, fitting more files on screen.",
                Category = "File Explorer",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "UseCompactMode",
                ValueType = "DWord",
                RecommendedValue = 1,
                DefaultValue = 0,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "explorer-launch-this-pc",
                Name = "Open File Explorer to 'This PC'",
                Description = "Opens File Explorer directly to This PC with drives listed, rather than the Home/Quick Access view.",
                Category = "File Explorer",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "LaunchTo",
                ValueType = "DWord",
                RecommendedValue = 1, // 1 = This PC
                DefaultValue = 2,     // 2 = Home
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "show-seconds-taskbar-clock",
                Name = "Show Seconds in Taskbar Clock",
                Description = "Displays precise seconds on the Windows 11 system tray clock.",
                Category = "Taskbar",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "ShowSecondsInSystemClock",
                ValueType = "DWord",
                RecommendedValue = 1,
                DefaultValue = 0,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "speed-up-menu-show-delay",
                Name = "Reduce Menu Show Delay",
                Description = "Reduces submenu hover animation latency from 400ms down to 150ms for snappier navigation.",
                Category = "Performance",
                RegistryHive = "HKCU",
                KeyPath = @"Control Panel\Desktop",
                ValueName = "MenuShowDelay",
                ValueType = "String",
                RecommendedValue = "150",
                DefaultValue = "400",
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-windows-tips",
                Name = "Disable Windows Tips & Tricks Popups",
                Description = "Disables intrusive notification popups offering tips and tricks about Windows features.",
                Category = "Notifications",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                ValueName = "SubscribedContent-310093Enabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-shake-minimize",
                Name = "Disable Aero Shake Window Minimizing",
                Description = "Prevents shaking a window from accidentally minimizing all other open application windows.",
                Category = "Window Management",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                ValueName = "DisallowShaking",
                ValueType = "DWord",
                RecommendedValue = 1,
                DefaultValue = 0,
                RiskLevel = OperationRiskLevel.Safe
            }
        };
    }

    public static List<OptimizationTweak> GetSupportedGamingCatalog()
    {
        return new List<OptimizationTweak>
        {
            new()
            {
                Id = "enable-game-mode",
                Name = "Ensure Windows Game Mode is Enabled",
                Description = "Prioritizes CPU and GPU resources for running games and suppresses background update restarts.",
                Category = "Gaming",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\GameBar",
                ValueName = "AllowAutoGameMode",
                ValueType = "DWord",
                RecommendedValue = 1,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-game-dvr-background-recording",
                Name = "Disable Game DVR Background Capture",
                Description = "Stops constant background disk buffering and encoding during gameplay, eliminating micro-stutters.",
                Category = "Gaming",
                RegistryHive = "HKCU",
                KeyPath = @"System\GameConfigStore",
                ValueName = "GameDVR_Enabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "disable-game-bar-app-capture",
                Name = "Disable Xbox App Capture Broadcasts",
                Description = "Prevents Game Bar from running background broadcast and capture services when gaming.",
                Category = "Gaming",
                RegistryHive = "HKCU",
                KeyPath = @"Software\Microsoft\Windows\CurrentVersion\GameDVR",
                ValueName = "AppCaptureEnabled",
                ValueType = "DWord",
                RecommendedValue = 0,
                DefaultValue = 1,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "enable-hags",
                Name = "Hardware-Accelerated GPU Scheduling (HAGS)",
                Description = "Allows dedicated GPU hardware to directly manage video memory scheduling, reducing latency and CPU overhead.",
                Category = "Graphics",
                RegistryHive = "HKLM",
                KeyPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                ValueName = "HwSchMode",
                ValueType = "DWord",
                RecommendedValue = 2, // 2 = Enabled, 1 = Disabled
                DefaultValue = 2,
                RequiresRestart = true,
                RiskLevel = OperationRiskLevel.Safe
            },
            new()
            {
                Id = "optimize-window-animations",
                Name = "Minimize Desktop Animation Latency",
                Description = "Disables window minimization animations for instantaneous transitions between fullscreen games and desktop.",
                Category = "Latency",
                RegistryHive = "HKCU",
                KeyPath = @"Control Panel\Desktop\WindowMetrics",
                ValueName = "MinAnimate",
                ValueType = "String",
                RecommendedValue = "0",
                DefaultValue = "1",
                RiskLevel = OperationRiskLevel.Safe
            }
        };
    }
}

