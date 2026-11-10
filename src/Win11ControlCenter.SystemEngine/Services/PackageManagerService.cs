using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Win11ControlCenter.Core.Enums;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;
using Win11ControlCenter.Core.Rules;
using Win11ControlCenter.PowerShell.Runner;

namespace Win11ControlCenter.SystemEngine.Services;

public class PackageManagerService : IPackageManagerService
{
    private readonly ILoggingService _logger;
    private readonly IOperationExecutor _operationExecutor;

    public PackageManagerService(ILoggingService logger, IOperationExecutor operationExecutor)
    {
        _logger = logger;
        _operationExecutor = operationExecutor;
    }

    public async Task<IReadOnlyList<ApplicationPackage>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default)
    {
        var packages = new List<ApplicationPackage>();

        try
        {
            // 1. Query Current User AppX/MSIX Packages via PowerShell runner (filtered for non-framework, removable user applications)
            const string script = "Get-AppxPackage | Where-Object { -not $_.IsFramework -and -not $_.NonRemovable } | Select-Object Name, PackageFullName, PackageFamilyName, Publisher, Version, InstallLocation | ConvertTo-Json -Compress";
            var (success, output, error) = await PowerShellCommandRunner.RunSafeCommandAsync(script, cancellationToken);

            if (success && !string.IsNullOrWhiteSpace(output))
            {
                using var doc = JsonDocument.Parse(output);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var pkg = ParseAppxPackage(elem);
                        if (pkg != null) packages.Add(pkg);
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var pkg = ParseAppxPackage(doc.RootElement);
                    if (pkg != null) packages.Add(pkg);
                }
            }

            // 2. Query Win32 Applications from Registry
            var win32Apps = GetWin32InstalledApplications();
            packages.AddRange(win32Apps);

            _logger.LogInfo($"Package discovery complete: {packages.Count} applications cataloged.", "PackageManager");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed during package discovery", ex, "PackageManager");
        }

        return packages.OrderBy(p => p.IsProtected).ThenBy(p => p.DisplayName).ToList();
    }

    public Task<IReadOnlyList<ApplicationPackage>> GetProvisionedPackagesAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<ApplicationPackage>();

        try
        {
            // Non-elevated discovery of provisioned packages via AppxAllUserStore
            using var storeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", false);
            if (storeKey != null)
            {
                foreach (var subKeyName in storeKey.GetSubKeyNames())
                {
                    var isProt = ProtectedPackages.IsProtected(subKeyName);
                    list.Add(new ApplicationPackage
                    {
                        Name = subKeyName,
                        DisplayName = FormatDisplayName(subKeyName),
                        PackageFullName = subKeyName,
                        PackageType = "Provisioned AppX",
                        InstallScope = "Provisioned (All Users)",
                        IsProvisioned = true,
                        IsProtected = isProt,
                        Category = isProt ? PackageCategory.ProtectedInfrastructure : CategorizePackage(subKeyName),
                        RiskLevel = isProt ? OperationRiskLevel.Critical : CalculateRisk(subKeyName)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Provisioned package scan failed", ex, "PackageManager");
        }

        return Task.FromResult<IReadOnlyList<ApplicationPackage>>(list);
    }

    public async Task<OperationPlan> CreateRemovalPlanAsync(IEnumerable<ApplicationPackage> packages, bool isDryRun)
    {
        var requestedItems = new List<OperationItem>();

        foreach (var pkg in packages.Where(p => p.IsSelectedForRemoval))
        {
            // SAFETY GATING: Re-check protection rules
            if (pkg.IsProtected || ProtectedPackages.IsProtected(pkg.Name) || 
                (!string.IsNullOrWhiteSpace(pkg.PackageFamilyName) && ProtectedPackages.IsProtected(pkg.PackageFamilyName)))
            {
                _logger.LogWarning($"Safety violation: Rejected attempt to add protected package '{pkg.DisplayName}' to removal plan.", "SafetyValidator");
                continue;
            }

            var item = new OperationItem
            {
                Name = $"Remove {pkg.DisplayName}",
                Description = $"Uninstall application package '{pkg.PackageFullName}'",
                Category = "App Debloater",
                RiskLevel = pkg.RiskLevel,
                OperationType = OperationType.PackageRemoval,
                Reversibility = ReversibilityTier.ReinstallRequired,
                Target = !string.IsNullOrWhiteSpace(pkg.PackageFullName) ? pkg.PackageFullName : pkg.Name,
                RecommendedValue = "Remove",
                RequiresAdmin = pkg.IsProvisioned || pkg.InstallScope.Contains("AllUsers"),
                Metadata = new Dictionary<string, string>
                {
                    { "PackageName", pkg.Name },
                    { "PackageFullName", pkg.PackageFullName },
                    { "PackageType", pkg.PackageType },
                    { "IsProvisioned", pkg.IsProvisioned.ToString() },
                    { "RemoveProvisioningToo", pkg.RemoveProvisioningToo.ToString() }
                }
            };

            requestedItems.Add(item);
        }

        return await _operationExecutor.GeneratePlanAsync(requestedItems, "Package Removal", isDryRun);
    }

    private static ApplicationPackage? ParseAppxPackage(JsonElement elem)
    {
        try
        {
            var name = elem.GetProperty("Name").GetString() ?? string.Empty;
            var fullName = elem.GetProperty("PackageFullName").GetString() ?? string.Empty;
            var familyName = elem.GetProperty("PackageFamilyName").GetString() ?? string.Empty;
            var publisher = elem.GetProperty("Publisher").GetString() ?? string.Empty;
            var version = elem.GetProperty("Version").GetString() ?? string.Empty;
            var installLocation = elem.GetProperty("InstallLocation").GetString();

            var isProtected = ProtectedPackages.IsProtected(name) || ProtectedPackages.IsProtected(familyName);
            var category = isProtected ? PackageCategory.ProtectedInfrastructure : CategorizePackage(name);
            var risk = isProtected ? OperationRiskLevel.Critical : CalculateRisk(name);

            long? sizeBytes = null;
            if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
            {
                try
                {
                    var dir = new DirectoryInfo(installLocation);
                    sizeBytes = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                }
                catch { }
            }

            var cleanPublisher = CleanPublisher(publisher);

            return new ApplicationPackage
            {
                Name = name,
                DisplayName = FormatDisplayName(name, cleanPublisher),
                PackageFullName = fullName,
                PackageFamilyName = familyName,
                Publisher = cleanPublisher,
                Version = version,
                InstallLocation = installLocation,
                PackageType = "AppX / MSIX",
                InstallScope = "CurrentUser",
                Category = category,
                RiskLevel = risk,
                IsProtected = isProtected,
                EstimatedSizeBytes = sizeBytes
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<ApplicationPackage> GetWin32InstalledApplications()
    {
        var apps = new List<ApplicationPackage>();
        var keys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var keyPath in keys)
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(keyPath, false);
            if (baseKey == null) continue;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                using var appKey = baseKey.OpenSubKey(subKeyName, false);
                if (appKey == null) continue;

                var displayName = appKey.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrWhiteSpace(displayName) || appKey.GetValue("SystemComponent") is 1)
                    continue;

                var publisher = appKey.GetValue("Publisher")?.ToString() ?? "Unknown";
                var version = appKey.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
                var installLoc = appKey.GetValue("InstallLocation")?.ToString();
                var sizeKb = appKey.GetValue("EstimatedSize");

                long? sizeBytes = sizeKb is int kb ? kb * 1024L : null;

                apps.Add(new ApplicationPackage
                {
                    Name = subKeyName,
                    DisplayName = displayName.Trim(),
                    Publisher = publisher.Trim(),
                    Version = version,
                    InstallLocation = installLoc,
                    PackageType = "Win32",
                    InstallScope = "Machine",
                    Category = PackageCategory.OptionalConsumer,
                    RiskLevel = OperationRiskLevel.Moderate,
                    IsProtected = false,
                    EstimatedSizeBytes = sizeBytes
                });
            }
        }

        return apps;
    }

    private static string FormatDisplayName(string rawName, string? publisher = null)
    {
        // 1. Strip Store Publisher ID prefix hash e.g. "15EF7777.Crunchyroll" -> "Crunchyroll", "5319275A.WhatsAppDesktop" -> "WhatsAppDesktop"
        var cleaned = Regex.Replace(rawName, @"^[0-9A-Za-z]{7,12}\.", "");

        // 2. Remove standard namespace stubs
        cleaned = cleaned.Replace("MicrosoftCorporationII.", "")
                         .Replace("MicrosoftWindows.", "")
                         .Replace("Microsoft.", "")
                         .Replace("Windows.", "");

        // 3. Known application name dictionary
        var mapped = cleaned switch
        {
            "BingNews" => "Microsoft News",
            "BingWeather" => "MSN Weather",
            "BingSearch" => "Bing Search",
            "MicrosoftSolitaireCollection" => "Microsoft Solitaire Collection",
            "Clipchamp.Clipchamp" or "Clipchamp" => "Clipchamp Video Editor",
            "GamingApp" => "Xbox App",
            "XboxGamingOverlay" => "Xbox Game Bar",
            "Xbox.TCUI" => "Xbox Live In-Game UI",
            "XboxIdentityProvider" => "Xbox Identity Provider",
            "XboxSpeechToTextOverlay" => "Xbox Speech-to-Text Overlay",
            "YourPhone" => "Phone Link",
            "Todos" => "Microsoft To Do",
            "PowerAutomateDesktop" => "Power Automate",
            "GetHelp" => "Get Help",
            "Getstarted" => "Tips / Get Started",
            "WindowsMaps" => "Windows Maps",
            "ZuneMusic" => "Windows Media Player",
            "ZuneVideo" => "Films & TV",
            "WindowsFeedbackHub" => "Feedback Hub",
            "MicrosoftOfficeHub" => "Microsoft 365 (Office)",
            "People" => "Microsoft People",
            "SkypeApp" => "Skype",
            "Cortana" or "549981C3F5F10" => "Cortana",
            "Crunchyroll" => "Crunchyroll",
            "WhatsAppDesktop" or "WhatsApp" => "WhatsApp Desktop",
            "AppUp.IntelGraphicsExperience" or "IntelGraphicsExperience" => "Intel Graphics Experience",
            "NVIDIACorp.NVIDIAControlPanel" or "NVIDIAControlPanel" => "NVIDIA Control Panel",
            "RealtekSemiconductorCorp.RealtekAudioControl" or "RealtekAudioControl" => "Realtek Audio Control",
            "AdvancedMicroDevicesInc-RSXCM" => "AMD Radeon Software Companion",
            "MicrosoftFamily" or "Family" => "Microsoft Family Safety",
            "QuickAssist" => "Quick Assist",
            "WindowsNotepad" or "Notepad" => "Notepad",
            "WindowsCalculator" or "Calculator" => "Calculator",
            "WindowsCamera" or "Camera" => "Camera",
            "WindowsAlarms" or "Alarms" => "Clock & Alarms",
            "WindowsSoundRecorder" or "SoundRecorder" => "Sound Recorder",
            "ScreenSketch" => "Snipping Tool",
            "MicrosoftStickyNotes" or "StickyNotes" => "Sticky Notes",
            "WindowsTerminal" or "Terminal" => "Windows Terminal",
            "Paint" => "Paint",
            "OutlookForWindows" => "Outlook for Windows",
            "PythonSoftwareFoundation.PythonManager" or "PythonManager" => "Python Manager",
            "RazerDynamicLighting" => "Razer Dynamic Lighting",
            "WidgetsPlatformRuntime" => "Windows Widgets Platform",
            "StartExperiencesApp" => "Windows Start Experience",
            "Client.WebExperience" or "WebExperience" => "Windows Web Experience",
            "CrossDevice" => "Cross Device Experience Host",
            "DevHome" => "Windows Dev Home",
            "StorePurchaseApp" => "Microsoft Store Purchase Service",
            "WindowsStore" => "Microsoft Store",
            "Winget.Source" => "Winget Package Source Engine",
            _ => null
        };

        if (mapped != null) return mapped;

        // 4. If package name is a raw GUID (e.g. sideloaded OEM stub like 65d483df-b37e...)
        if (Regex.IsMatch(cleaned, @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-"))
        {
            if (!string.IsNullOrWhiteSpace(publisher) && !publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                var pubClean = publisher.Trim('\"', ' ');
                return $"{pubClean} Component";
            }
            return $"OEM Package ({cleaned.Substring(0, 8)}...)";
        }

        // 5. Expand CamelCase to readable words (e.g. "RealtekAudioControl" -> "Realtek Audio Control")
        var formatted = Regex.Replace(cleaned, @"([a-z])([A-Z])", "$1 $2");
        formatted = Regex.Replace(formatted, @"([A-Z]+)([A-Z][a-z])", "$1 $2");
        return formatted.Trim();
    }

    private static string CleanPublisher(string rawPublisher)
    {
        if (rawPublisher.Contains("CN=Microsoft", StringComparison.OrdinalIgnoreCase))
            return "Microsoft Corporation";

        var cnIdx = rawPublisher.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
        if (cnIdx >= 0)
        {
            var endIdx = rawPublisher.IndexOf(',', cnIdx);
            return endIdx > cnIdx ? rawPublisher.Substring(cnIdx + 3, endIdx - cnIdx - 3) : rawPublisher.Substring(cnIdx + 3);
        }

        return string.IsNullOrWhiteSpace(rawPublisher) ? "Unknown" : rawPublisher.Trim('\"', ' ');
    }

    private static PackageCategory CategorizePackage(string name)
    {
        if (name.Contains("News", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Weather", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("BingSearch", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Solitaire", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Clipchamp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Zune", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FeedbackHub", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("GetHelp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Getstarted", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("WindowsMaps", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("OfficeHub", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("People", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("SkypeApp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PowerAutomate", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Todos", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Cortana", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("549981C3F5F10", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Crunchyroll", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Spotify", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Netflix", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Disney", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("TikTok", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Duolingo", StringComparison.OrdinalIgnoreCase))
        {
            return PackageCategory.SafeConsumer;
        }

        if (name.Contains("Xbox", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("YourPhone", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("GamingApp", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Teams", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("OneDrive", StringComparison.OrdinalIgnoreCase))
        {
            return PackageCategory.OptionalConsumer;
        }

        return PackageCategory.AdvancedCandidate;
    }

    private static OperationRiskLevel CalculateRisk(string name)
    {
        return CategorizePackage(name) switch
        {
            PackageCategory.SafeConsumer => OperationRiskLevel.Safe,
            PackageCategory.OptionalConsumer => OperationRiskLevel.Moderate,
            PackageCategory.AdvancedCandidate => OperationRiskLevel.Advanced,
            _ => OperationRiskLevel.Advanced
        };
    }
}
