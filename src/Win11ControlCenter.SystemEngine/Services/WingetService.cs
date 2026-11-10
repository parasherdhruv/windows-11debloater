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
            // === ESSENTIAL UTILITIES (FOSS & Free) ===
            new() { 
                Id = "7zip.7zip", 
                Name = "7-Zip", 
                Publisher = "Igor Pavlov", 
                Category = "Utilities", 
                Description = "High-ratio open-source file archiver supporting 7z, ZIP, TAR, GZ, and RAR archives.", 
                IconGlyph = "🗜️", 
                HomepageUrl = "https://www.7-zip.org",
                License = "Open Source (LGPL / BSD)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "voidtools.Everything", 
                Name = "Everything Search", 
                Publisher = "voidtools", 
                Category = "Utilities", 
                Description = "Instant desktop filename search engine indexing millions of files with negligible RAM overhead.", 
                IconGlyph = "⚡", 
                HomepageUrl = "https://www.voidtools.com",
                License = "Freeware (100% Free)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Microsoft.PowerToys", 
                Name = "Microsoft PowerToys", 
                Publisher = "Microsoft Corporation", 
                Category = "Utilities", 
                Description = "Windows system power suite including FancyZones, Text Extractor, Image Resizer, and Awake.", 
                IconGlyph = "🛠️", 
                HomepageUrl = "https://github.com/microsoft/PowerToys",
                License = "Open Source (MIT)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Notepad++.Notepad++", 
                Name = "Notepad++", 
                Publisher = "Don Ho", 
                Category = "Utilities", 
                Description = "Ultra-fast open-source source code and text editor. Reliable replacement for Windows Notepad.", 
                IconGlyph = "📝", 
                HomepageUrl = "https://notepad-plus-plus.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "ShareX.ShareX", 
                Name = "ShareX", 
                Publisher = "ShareX Team", 
                Category = "Utilities", 
                Description = "Free and open-source screen capture, screen recorder, OCR text grabber, and productivity tool.", 
                IconGlyph = "📸", 
                HomepageUrl = "https://getsharex.com",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "FastStone.Viewer", 
                Name = "FastStone Image Viewer", 
                Publisher = "FastStone Soft", 
                Category = "Utilities", 
                Description = "Fast, lightweight image viewer, converter, and thumbnail browser with zero bloat.", 
                IconGlyph = "🖼️", 
                HomepageUrl = "https://www.faststone.org",
                License = "Freeware (Free for Personal)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Rufus.Rufus", 
                Name = "Rufus", 
                Publisher = "Pete Batard", 
                Category = "Utilities", 
                Description = "Reliable utility to format and create bootable USB drives (Windows 11, Linux ISOs, UEFI).", 
                IconGlyph = "💾", 
                HomepageUrl = "https://rufus.ie",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "CrystalDewWorld.CrystalDiskInfo", 
                Name = "CrystalDiskInfo", 
                Publisher = "Crystal Dew World", 
                Category = "Utilities", 
                Description = "HDD/SSD drive health and temperature monitoring utility supporting S.M.A.R.T. and NVMe drives.", 
                IconGlyph = "🩺", 
                HomepageUrl = "https://crystalmark.info",
                License = "Open Source (MIT)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "AntibodySoftware.WizTree", 
                Name = "WizTree", 
                Publisher = "Antibody Software", 
                Category = "Utilities", 
                Description = "The world's fastest disk space visualizer and storage clutter analyzer reading MFT directly.", 
                IconGlyph = "📊", 
                HomepageUrl = "https://diskanalyzer.com",
                License = "Freeware (Free for Personal)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "AutoHotkey.AutoHotkey", 
                Name = "AutoHotkey", 
                Publisher = "Lexikos", 
                Category = "Utilities", 
                Description = "Ultimate Windows macro-creation, keystroke automation, and custom hotkey scripting language.", 
                IconGlyph = "⌨️", 
                HomepageUrl = "https://www.autohotkey.com",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === WEB BROWSERS (Privacy & FOSS) ===
            new() { 
                Id = "Mozilla.Firefox", 
                Name = "Mozilla Firefox", 
                Publisher = "Mozilla Foundation", 
                Category = "Browsers", 
                Description = "Privacy-respecting, extensible open-source browser. Independent Quantum engine with zero tracking.", 
                IconGlyph = "🦊", 
                HomepageUrl = "https://www.mozilla.org/firefox",
                License = "Open Source (MPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Brave.Brave", 
                Name = "Brave Browser", 
                Publisher = "Brave Software", 
                Category = "Browsers", 
                Description = "Fast Chromium-based browser with built-in Shields ad-blocker, tracker blocking, and Tor tabs.", 
                IconGlyph = "🦁", 
                HomepageUrl = "https://brave.com",
                License = "Open Source (MPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "LibreWolf.LibreWolf", 
                Name = "LibreWolf", 
                Publisher = "LibreWolf Community", 
                Category = "Browsers", 
                Description = "Custom-hardened Firefox fork pre-configured for maximum privacy, no telemetry, and anti-fingerprinting.", 
                IconGlyph = "🐺", 
                HomepageUrl = "https://librewolf.net",
                License = "Open Source (MPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "TorProject.TorBrowser", 
                Name = "Tor Browser", 
                Publisher = "The Tor Project", 
                Category = "Browsers", 
                Description = "Defend yourself against tracking and surveillance. Routes traffic through encrypted onion network.", 
                IconGlyph = "🧅", 
                HomepageUrl = "https://www.torproject.org",
                License = "Open Source (BSD-3-Clause)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === DEVELOPER TOOLS ===
            new() { 
                Id = "Microsoft.VisualStudioCode", 
                Name = "Visual Studio Code", 
                Publisher = "Microsoft Corporation", 
                Category = "Developer", 
                Description = "Modern multi-language code editor with Git integration, debugging, and thousands of extensions.", 
                IconGlyph = "💻", 
                HomepageUrl = "https://code.visualstudio.com",
                License = "Free Software (MIT Engine)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Git.Git", 
                Name = "Git for Windows", 
                Publisher = "The Git Development Community", 
                Category = "Developer", 
                Description = "Industry standard distributed version control system with Git Bash and Windows Credential Manager.", 
                IconGlyph = "🌿", 
                HomepageUrl = "https://git-scm.com",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Microsoft.WindowsTerminal", 
                Name = "Windows Terminal", 
                Publisher = "Microsoft Corporation", 
                Category = "Developer", 
                Description = "Modern, GPU-accelerated tabbed terminal emulator for PowerShell, Command Prompt, and WSL.", 
                IconGlyph = "📟", 
                HomepageUrl = "https://github.com/microsoft/terminal",
                License = "Open Source (MIT)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "OpenJS.NodeJS.LTS", 
                Name = "Node.js (LTS)", 
                Publisher = "OpenJS Foundation", 
                Category = "Developer", 
                Description = "Premier JavaScript and TypeScript runtime built on Chrome's V8 engine with npm package manager.", 
                IconGlyph = "🟢", 
                HomepageUrl = "https://nodejs.org",
                License = "Open Source (MIT)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Python.Python.3.12", 
                Name = "Python 3.12", 
                Publisher = "Python Software Foundation", 
                Category = "Developer", 
                Description = "Modern high-level general-purpose programming language interpreter with pip package manager.", 
                IconGlyph = "🐍", 
                HomepageUrl = "https://www.python.org",
                License = "Open Source (PSF License)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "GoLang.Go", 
                Name = "Go Programming Language", 
                Publisher = "Google LLC / Go Authors", 
                Category = "Developer", 
                Description = "Fast, statically typed, compiled language with built-in concurrency for cloud and systems services.", 
                IconGlyph = "🐹", 
                HomepageUrl = "https://go.dev",
                License = "Open Source (BSD-3-Clause)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Rustlang.Rustup", 
                Name = "Rust (rustup)", 
                Publisher = "Rust Foundation", 
                Category = "Developer", 
                Description = "Memory-safe systems programming language installer, compiler (rustc), and package manager (cargo).", 
                IconGlyph = "🦀", 
                HomepageUrl = "https://rustup.rs",
                License = "Open Source (MIT / Apache-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Neovim.Neovim", 
                Name = "Neovim", 
                Publisher = "Neovim Team", 
                Category = "Developer", 
                Description = "Vim-fork focused on extensibility, Lua configuration, asynchronous architecture, and speed.", 
                IconGlyph = "💤", 
                HomepageUrl = "https://neovim.io",
                License = "Open Source (Apache-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "dbeaver.dbeaver", 
                Name = "DBeaver Community", 
                Publisher = "DBeaver Corp", 
                Category = "Developer", 
                Description = "Universal database management tool and SQL client supporting PostgreSQL, MySQL, SQLite, and more.", 
                IconGlyph = "🦫", 
                HomepageUrl = "https://dbeaver.io",
                License = "Open Source (Apache-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === MEDIA & AUDIO ===
            new() { 
                Id = "VideoLAN.VLC", 
                Name = "VLC Media Player", 
                Publisher = "VideoLAN Organization", 
                Category = "Media", 
                Description = "Universal open-source media player. Plays all video and audio formats natively without codec packs.", 
                IconGlyph = "🎬", 
                HomepageUrl = "https://www.videolan.org/vlc",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "OBSProject.OBSStudio", 
                Name = "OBS Studio", 
                Publisher = "OBS Project", 
                Category = "Media", 
                Description = "Premier open-source broadcasting and screen recording suite with real-time video/audio mixing.", 
                IconGlyph = "🎥", 
                HomepageUrl = "https://obsproject.com",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "HandBrake.HandBrake", 
                Name = "HandBrake", 
                Publisher = "The HandBrake Team", 
                Category = "Media", 
                Description = "Open-source video transcoder for converting video files into modern MP4, MKV, AV1, and WebM formats.", 
                IconGlyph = "🎞️", 
                HomepageUrl = "https://handbrake.fr",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Audacity.Audacity", 
                Name = "Audacity", 
                Publisher = "Muse Group / Audacity Team", 
                Category = "Media", 
                Description = "Famous open-source multi-track audio recorder, noise reducer, and sound editor.", 
                IconGlyph = "🎙️", 
                HomepageUrl = "https://www.audacityteam.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "SumatraPDF.SumatraPDF", 
                Name = "Sumatra PDF", 
                Publisher = "Krzysztof Kowalczyk", 
                Category = "Media", 
                Description = "Featherlight, instant-launch PDF, eBook (ePub, MOBI), and comic book reader with zero bloat.", 
                IconGlyph = "📖", 
                HomepageUrl = "https://www.sumatrapdfreader.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "mifi.lossless-cut", 
                Name = "LosslessCut", 
                Publisher = "Mikael Finstad", 
                Category = "Media", 
                Description = "Swiss army knife for lossless video and audio trimming, cutting, and slicing without re-encoding.", 
                IconGlyph = "✂️", 
                HomepageUrl = "https://github.com/mifi/lossless-cut",
                License = "Open Source (GPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === GRAPHICS & CREATIVE DESIGN ===
            new() { 
                Id = "GIMP.GIMP", 
                Name = "GIMP", 
                Publisher = "The GIMP Development Team", 
                Category = "Graphics", 
                Description = "GNU Image Manipulation Program. Free, open-source alternative to Photoshop for photo editing.", 
                IconGlyph = "🎨", 
                HomepageUrl = "https://www.gimp.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "BlenderFoundation.Blender", 
                Name = "Blender 3D", 
                Publisher = "Blender Foundation", 
                Category = "Graphics", 
                Description = "World-class 3D creation suite: modeling, rigging, animation, simulation, rendering, and video editing.", 
                IconGlyph = "🧊", 
                HomepageUrl = "https://www.blender.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "KDE.Krita", 
                Name = "Krita", 
                Publisher = "Krita Foundation / Stichting Krita", 
                Category = "Graphics", 
                Description = "Professional open-source digital painting, concept art, texture, and comic illustration studio.", 
                IconGlyph = "🖌️", 
                HomepageUrl = "https://krita.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Inkscape.Inkscape", 
                Name = "Inkscape", 
                Publisher = "Inkscape Community", 
                Category = "Graphics", 
                Description = "Professional vector graphics editor for SVG illustrations, diagrams, line arts, and logos.", 
                IconGlyph = "✒️", 
                HomepageUrl = "https://inkscape.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === SECURITY & PRIVACY ===
            new() { 
                Id = "Bitwarden.Bitwarden", 
                Name = "Bitwarden", 
                Publisher = "Bitwarden Inc.", 
                Category = "Security", 
                Description = "Top-rated open-source zero-knowledge password vault and authenticator for all devices.", 
                IconGlyph = "🛡️", 
                HomepageUrl = "https://bitwarden.com",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "KeePassXCTeam.KeePassXC", 
                Name = "KeePassXC", 
                Publisher = "KeePassXC Team", 
                Category = "Security", 
                Description = "Offline community-driven password manager. Keeps your credentials in local encrypted database files.", 
                IconGlyph = "🔑", 
                HomepageUrl = "https://keepassxc.org",
                License = "Open Source (GPL-2.0 / GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Proton.ProtonVPN", 
                Name = "Proton VPN", 
                Publisher = "Proton AG", 
                Category = "Security", 
                Description = "Swiss-based high-speed privacy VPN with strict no-logs policy and unlimited free server tier.", 
                IconGlyph = "🌐", 
                HomepageUrl = "https://protonvpn.com",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "IDRIX.VeraCrypt", 
                Name = "VeraCrypt", 
                Publisher = "IDRIX", 
                Category = "Security", 
                Description = "Free open-source disk encryption software based on TrueCrypt. Creates encrypted virtual drives.", 
                IconGlyph = "🔒", 
                HomepageUrl = "https://www.veracrypt.fr",
                License = "Open Source (Apache-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "BleachBit.BleachBit", 
                Name = "BleachBit", 
                Publisher = "Andrew Ziem", 
                Category = "Security", 
                Description = "Open-source system privacy cleaner. Deletes tracking cookies, internet history, and shreds junk files.", 
                IconGlyph = "🧹", 
                HomepageUrl = "https://www.bleachbit.org",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },

            // === PRODUCTIVITY & OFFICE ===
            new() { 
                Id = "TheDocumentFoundation.LibreOffice", 
                Name = "LibreOffice", 
                Publisher = "The Document Foundation", 
                Category = "Productivity", 
                Description = "Powerful open-source office suite: Writer (Word), Calc (Excel), Impress (PPT), and Draw.", 
                IconGlyph = "📑", 
                HomepageUrl = "https://www.libreoffice.org",
                License = "Open Source (MPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Obsidian.Obsidian", 
                Name = "Obsidian", 
                Publisher = "Dynalist Inc.", 
                Category = "Productivity", 
                Description = "Sharp, extensible knowledge base and note-taking app that works on your local Markdown folder.", 
                IconGlyph = "💎", 
                HomepageUrl = "https://obsidian.md",
                License = "Freeware (Free for Personal Use)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Mozilla.Thunderbird", 
                Name = "Mozilla Thunderbird", 
                Publisher = "MZLA Technologies Corporation", 
                Category = "Productivity", 
                Description = "Free, open-source email, calendar, and contacts client customized for privacy and productivity.", 
                IconGlyph = "📬", 
                HomepageUrl = "https://www.thunderbird.net",
                License = "Open Source (MPL-2.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "KovidGoyal.Calibre", 
                Name = "Calibre E-Book Manager", 
                Publisher = "Kovid Goyal", 
                Category = "Productivity", 
                Description = "The comprehensive open-source e-book manager, viewer, and format converter (EPUB, PDF, Kindle).", 
                IconGlyph = "📚", 
                HomepageUrl = "https://calibre-ebook.com",
                License = "Open Source (GPL-3.0)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            },
            new() { 
                Id = "Joplin.Joplin", 
                Name = "Joplin", 
                Publisher = "Laurent Cozic", 
                Category = "Productivity", 
                Description = "Secure, open-source note taking and to-do application with end-to-end encryption synchronization.", 
                IconGlyph = "🗒️", 
                HomepageUrl = "https://joplinapp.org",
                License = "Open Source (MIT)",
                RepositorySource = "Official Winget Community (winget-pkgs)"
            }
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

                try
                {
                    await proc.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
                }
                catch
                {
                    try { if (!proc.HasExited) proc.Kill(true); } catch { }
                }
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

            try
            {
                await proc.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromMinutes(5), ct);
                await readTask;
            }
            finally
            {
                try
                {
                    if (!proc.HasExited) proc.Kill(true);
                }
                catch { }
            }

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
