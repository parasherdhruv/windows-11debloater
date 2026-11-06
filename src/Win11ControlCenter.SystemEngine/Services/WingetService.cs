using System.Diagnostics;
using System.Text.RegularExpressions;
using Win11ControlCenter.Core.Interfaces;
using Win11ControlCenter.Core.Models;

namespace Win11ControlCenter.SystemEngine.Services;

public partial class WingetService : IWingetService
{
    private readonly ILoggingService _logger;
    private static readonly Regex SafeIdRegex = SafeIdRegexGenerator();

    public WingetService(ILoggingService logger)
    {
        _logger = logger;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\.\-_+]+$")]
    private static partial Regex SafeIdRegexGenerator();

    public async Task<bool> IsWingetAvailableAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return false;

            await proc.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            return proc.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Winget is not available on this system: {ex.Message}", "WingetService");
            return false;
        }
    }

    public Task<List<SoftwarePackageItem>> GetCuratedPackagesAsync()
    {
        var list = new List<SoftwarePackageItem>
        {
            // Essential Utilities
            new() { Id = "7zip.7zip", Name = "7-Zip", Publisher = "Igor Pavlov", Category = "Utilities", Description = "High-ratio open-source file archiver supporting 7z, ZIP, TAR, GZ, and RAR.", IconGlyph = "🗜️", HomepageUrl = "https://www.7-zip.org" },
            new() { Id = "voidtools.Everything", Name = "Everything Search", Publisher = "voidtools", Category = "Utilities", Description = "Instant desktop filename search engine with negligible RAM overhead.", IconGlyph = "🔍", HomepageUrl = "https://www.voidtools.com" },
            new() { Id = "Microsoft.PowerToys", Name = "Microsoft PowerToys", Publisher = "Microsoft Corporation", Category = "Utilities", Description = "System utility suite including FancyZones, Text Extractor, and Awake.", IconGlyph = "⚡", HomepageUrl = "https://github.com/microsoft/PowerToys" },
            new() { Id = "Notepad++.Notepad++", Name = "Notepad++", Publisher = "Don Ho", Category = "Utilities", Description = "Ultra-fast open-source source code editor and replacement for Notepad.", IconGlyph = "📝", HomepageUrl = "https://notepad-plus-plus.org" },
            new() { Id = "ShareX.ShareX", Name = "ShareX", Publisher = "ShareX Team", Category = "Utilities", Description = "Free and open-source screen capture, screen recorder, and file sharing tool.", IconGlyph = "📸", HomepageUrl = "https://getsharex.com" },
            new() { Id = "FastStone.Viewer", Name = "FastStone Image Viewer", Publisher = "FastStone Soft", Category = "Utilities", Description = "Fast, user-friendly, full-featured image viewer and editor.", IconGlyph = "🖼️", HomepageUrl = "https://www.faststone.org" },

            // Web Browsers
            new() { Id = "Mozilla.Firefox", Name = "Mozilla Firefox", Publisher = "Mozilla", Category = "Browsers", Description = "Privacy-respecting, extensible open-source web browser without telemetry bloat.", IconGlyph = "🦊", HomepageUrl = "https://www.mozilla.org/firefox" },
            new() { Id = "Brave.Brave", Name = "Brave Browser", Publisher = "Brave Software", Category = "Browsers", Description = "High-performance Chromium browser with built-in Shields ad and tracker blocker.", IconGlyph = "🦁", HomepageUrl = "https://brave.com" },
            new() { Id = "LibreWolf.LibreWolf", Name = "LibreWolf", Publisher = "LibreWolf Community", Category = "Browsers", Description = "Custom-hardened Firefox fork focused on privacy, security and freedom.", IconGlyph = "🐺", HomepageUrl = "https://librewolf.net" },

            // Developer Tools
            new() { Id = "Microsoft.VisualStudioCode", Name = "Visual Studio Code", Publisher = "Microsoft Corporation", Category = "Developer", Description = "Code editing redefined. Powerful, extensible multi-language developer IDE.", IconGlyph = "💻", HomepageUrl = "https://code.visualstudio.com" },
            new() { Id = "Git.Git", Name = "Git for Windows", Publisher = "The Git Development Community", Category = "Developer", Description = "Standard distributed version control system with Git Bash and credential manager.", IconGlyph = "🌿", HomepageUrl = "https://git-scm.com" },
            new() { Id = "Microsoft.WindowsTerminal", Name = "Windows Terminal", Publisher = "Microsoft Corporation", Category = "Developer", Description = "Modern, tabbed, fast terminal application for PowerShell, CMD, and WSL.", IconGlyph = "📟", HomepageUrl = "https://github.com/microsoft/terminal" },
            new() { Id = "OpenJS.NodeJS.LTS", Name = "Node.js (LTS)", Publisher = "OpenJS Foundation", Category = "Developer", Description = "JavaScript runtime environment built on Chrome's V8 engine.", IconGlyph = "🟢", HomepageUrl = "https://nodejs.org" },
            new() { Id = "Python.Python.3.12", Name = "Python 3.12", Publisher = "Python Software Foundation", Category = "Developer", Description = "Modern high-level general-purpose programming language interpreter.", IconGlyph = "🐍", HomepageUrl = "https://www.python.org" },

            // Media & Audio
            new() { Id = "VideoLAN.VLC", Name = "VLC Media Player", Publisher = "VideoLAN", Category = "Media", Description = "The definitive open-source universal media player. Plays all codecs natively.", IconGlyph = "🎬", HomepageUrl = "https://www.videolan.org/vlc" },
            new() { Id = "OBSProject.OBSStudio", Name = "OBS Studio", Publisher = "OBS Project", Category = "Media", Description = "Open source software for video recording and live broadcasting.", IconGlyph = "🎥", HomepageUrl = "https://obsproject.com" },
            new() { Id = "HandBrake.HandBrake", Name = "HandBrake", Publisher = "The HandBrake Team", Category = "Media", Description = "Open-source video transcoder for converting video from nearly any format.", IconGlyph = "🎞️", HomepageUrl = "https://handbrake.fr" },
            new() { Id = "SumatraPDF.SumatraPDF", Name = "Sumatra PDF", Publisher = "Krzysztof Kowalczyk", Category = "Media", Description = "Tiny, portable, lightning-fast PDF, eBook, and comic book reader.", IconGlyph = "📖", HomepageUrl = "https://www.sumatrapdfreader.org" },

            // Security & Privacy Tools
            new() { Id = "Bitwarden.Bitwarden", Name = "Bitwarden", Publisher = "Bitwarden Inc.", Category = "Security", Description = "Open-source zero-knowledge password and secrets manager.", IconGlyph = "🛡️", HomepageUrl = "https://bitwarden.com" },
            new() { Id = "Obsidian.Obsidian", Name = "Obsidian", Publisher = "Dynalist Inc.", Category = "Productivity", Description = "Powerful and extensible knowledge base that works on local Markdown files.", IconGlyph = "💎", HomepageUrl = "https://obsidian.md" }
        };

        return Task.FromResult(list);
    }

    public async Task RefreshInstalledStatusAsync(IEnumerable<SoftwarePackageItem> packages)
    {
        try
        {
            var installedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                string? line;
                while ((line = await proc.StandardOutput.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        foreach (var pkg in packages)
                        {
                            if (line.Contains(pkg.Id, StringComparison.OrdinalIgnoreCase))
                            {
                                installedIds.Add(pkg.Id);
                            }
                        }
                    }
                }

                await proc.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
            }

            foreach (var pkg in packages)
            {
                bool isInstalled = installedIds.Contains(pkg.Id);
                pkg.IsInstalled = isInstalled;
                pkg.InstallStatus = isInstalled ? "Installed" : "Not Installed";
                pkg.StatusMessage = isInstalled ? "Already installed on system" : "Ready to install";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to refresh package installation status from winget", ex, "WingetService");
        }
    }

    public async Task<bool> InstallPackageAsync(SoftwarePackageItem package, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!SafeIdRegex.IsMatch(package.Id))
        {
            _logger.LogError($"Rejected installation for invalid package ID: '{package.Id}'", null, "WingetService");
            package.InstallStatus = "Error";
            package.StatusMessage = "Invalid package identifier.";
            return false;
        }

        package.InstallStatus = "Installing";
        package.StatusMessage = "Downloading and installing via Winget...";
        progress?.Report($"[Winget] Starting install for {package.Name} ({package.Id})...");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = $"install --id {package.Id} --exact --silent --accept-source-agreements --accept-package-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                package.InstallStatus = "Failed";
                package.StatusMessage = "Failed to launch winget process.";
                return false;
            }

            // Read output asynchronously to prevent buffer lockup and report progress
            var readTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var trimmed = line.Trim();
                        progress?.Report($"[Winget] {trimmed}");
                        _logger.LogInfo($"Winget [{package.Id}]: {trimmed}", "WingetService");
                    }
                }
            }, ct);

            await proc.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromMinutes(5), ct);
            await readTask;

            if (proc.ExitCode == 0)
            {
                package.IsInstalled = true;
                package.InstallStatus = "Installed";
                package.StatusMessage = "Successfully installed!";
                progress?.Report($"[Winget] {package.Name} installed successfully!");
                return true;
            }
            else
            {
                package.InstallStatus = "Failed";
                package.StatusMessage = $"Installation exited with code {proc.ExitCode}";
                progress?.Report($"[Winget] Installation failed (code {proc.ExitCode}).");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error installing package {package.Id}", ex, "WingetService");
            package.InstallStatus = "Error";
            package.StatusMessage = $"Error: {ex.Message}";
            progress?.Report($"[Winget] Exception: {ex.Message}");
            return false;
        }
    }
}
