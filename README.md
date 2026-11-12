# Win11 Control Center v1.2.0

A modern, modular, and safety-first Windows 11 debloater, optimization dashboard, software installer, and system configuration center built in C# and .NET 10 LTS with WPF, MVVM (`CommunityToolkit.Mvvm`), and Dependency Injection.

![Platform](https://img.shields.io/badge/Platform-Windows%2011%20x64-blue)
![Framework](https://img.shields.io/badge/.NET-10.0%20LTS-purple)
![Architecture](https://img.shields.io/badge/Architecture-WPF%20%7C%20MVVM-green)
![Tests](https://img.shields.io/badge/Tests-145%20Passing%20(100%25)-brightgreen)
![License](https://img.shields.io/badge/License-MIT-orange)

---

## Core Philosophy

1. **Safety before performance**: Never disable or remove critical system infrastructure or core security providers.
2. **Reversibility before convenience**: Every configuration change is backed up before modification with automatic rollback readiness.
3. **Transparency before automation**: Detailed dry-run simulations, state diff previews, and selected items surfacing before execution.
4. **User consent before system modifications**: Explicit review dialogs with clear risk tiers and dry-run toggles.
5. **Measurable optimization instead of fake performance claims**: Genuine Windows APIs, performance counters, and registry keys; zero fake RAM-flushes or simulated FPS boosts.
6. **Modular architecture instead of hardcoded scripts**: Clear separation across Core, Infrastructure, PowerShell, SystemEngine, and Presentation layers.

---

## Complete 13-Module Feature Set

1. **📊 Intelligent Dashboard**: Genuine hardware detection (CPU model, GPU model, RAM via `GlobalMemoryStatusEx`, storage via `DriveInfo`, active power scheme, administrator elevation status, and pending reboot flags from CBS and Windows Update). Read-only scan with zero system modifications.
2. **🗑️ Application Debloater**: Discovers installed AppX, MSIX, Provisioned packages (`AppxAllUserStore`), and traditional Win32 applications. Differentiates user profile installs from provisioned packages. Features an interactive **"Selected for Removal" Quick Shelf / Tray**, segmented tabs (`All Applications`, `Selected Only`, `Safe Recommendations`), and auto-pinning selected items to top. Protected package shields strictly prevent removal of Windows Shell, Store, Defender, or VCLibs dependencies.
3. **📦 Essential Software Center (Winget)**: Curated bloat-free open-source software installer powered by the official Windows Package Manager (`winget.exe`). Allows single-click or batch installation of essential utilities (7-Zip, Notepad++, Everything Search, PowerToys, ShareX, FastStone), browsers (Firefox, Brave, LibreWolf), developer tools (VS Code, Git, Windows Terminal, Node.js, Python), media players (VLC, OBS Studio, HandBrake, Sumatra PDF), and privacy/notes tools (Bitwarden, Obsidian). Includes live progress streaming and strict Regex ID sanitization.
4. **🚀 Startup Manager**: Audits Run registry keys (`HKCU`/`HKLM`) and Startup folders. Non-destructive enabling and disabling via `StartupApproved\Run` bitmasks (`0x02` vs `0x03`). Never deletes binary files.
5. **⚙️ Windows Services Manager**: Comprehensive service manager with dependency graph inspection, startup type configuration (Automatic, Manual, Disabled), and service start/stop controls. Hardcoded safety shields protect 28 core Windows infrastructure services (`wuauserv`, `RpcSs`, `WinDefend`, `CryptSvc`, `Dhcp`, `Dnscache`, `AppXSvc`, etc.).
6. **🔒 Privacy & Telemetry**: Manages 10 standard Windows privacy settings (Advertising ID, Tailored Experiences, Diagnostic Data minimization to required baseline, Activity History cloud sync, Bing search in Start, App launch tracking, Settings suggestions) using documented registry mechanisms.
7. **✨ Windows Tweaks & Optimization**: 5 predefined optimization profiles (Safe Cleanup, Balanced Optimization, Gaming Optimization, Privacy Focused, Minimal Windows) + an interactive Live Profile State Diff Inspector. Includes File Explorer usability tweaks (show extensions, show hidden files, compact view, reduce menu show delay) and `powercfg` scheme switching.
8. **🎮 Gaming Mode Optimization**: Hardware-Accelerated GPU Scheduling (HAGS), Windows Game Mode prioritization, Game DVR background MP4 capture buffering elimination (preventing micro-stutters during disk-heavy scenes), Xbox broadcast capture suppression, and High Performance power scheme alignment.
9. **🧹 System Cleaner & Maintenance**: Safely cleans temporary caches (User Temp, Windows Temp, Thumbnail Cache, Crash Dumps, Delivery Optimization) and empties the Recycle Bin using Win32 Shell APIs (`SHQueryRecycleBin` and `SHEmptyRecycleBin`). Features a **"Selected Targets Quick Shelf"** with real-time size calculations, 2-hour file age grace period, and strict shielding of personal folders (`Documents`, `Desktop`, `Downloads`, `Pictures`, `Music`, `Videos`, `System32`, `WinSxS`).
10. **🛠️ Custom Tweaks**: User-defined registry tweaks builder supporting `HKCU` and `HKLM`, subkeys, value names, types (`DWord`, `String`, `QWord`), and target/default values. Real-time safety validation blocks attempts to target critical security or boot hives (`SAM`, `SECURITY`, `BCD00000000`, `SafeBoot`, `KnownDLLs`).
11. **🛡️ Restore Center**: Pre-optimization snapshot repository with collision-resistant timestamps and directory isolation. Generates emergency `pre_apply_rollback.reg` files for offline registry rollback directly through Windows Explorer. Integrates with Windows System Restore (`Checkpoint-Computer`) with description sanitization.
12. **📜 Activity Logs**: Persistent JSONL audit trail (`audit.jsonl`) recording every operation, status (Succeeded, Failed, Skipped, RolledBack), and message. Includes status severity filtering, category filtering, search, and JSON export.
13. **⚙️ Settings & Appearance**: Runtime theme switching between Fluent Dark Mode (Default) and Light Mode. Configuration for startup audits, restore point creation, and audit retention policies stored in `%APPDATA%\Win11ControlCenter\settings.json`.

---

## 10-Step Safe Execution Workflow

All system modifications adhere to a strict 10-step lifecycle:

```
[1. Discover]   -> Enumerate targets using read-only APIs
[2. Analyze]    -> Evaluate safety rules and risk classifications
[3. Plan]       -> Build strongly-typed OperationPlan
[4. Preview]    -> Present operation details and parameters to user
[5. Backup]     -> Generate emergency .reg and state snapshots
[6. Approve]    -> Require explicit confirmation for irreversible operations
[7. Execute]    -> Apply modifications through typed, safe handlers
[8. Verify]     -> Query post-execution state to confirm application
[9. Log]        -> Record structured operation result to audit history
[10. Restore]   -> Retain rollback plan and snapshot for one-click restore
```

---

## Automated Test Suite

Win11 Control Center includes **145 automated tests** passing with a 100% pass rate:

- **91 Safety Tests** (`Win11ControlCenter.SafetyTests`):
  - Protected packages removal prevention (`ShellExperienceHost`, `SecHealthUI`, `StorePurchaseApp`, etc.)
  - Core services preservation (`wuauserv`, `RpcSs`, `WinDefend`, `CryptSvc`, `Dhcp`, etc.)
  - Dangerous registry targets rejection (`SAM`, `SECURITY`, `BCD00000000`, `SafeBoot`, `KnownDLLs`)
  - Directory cleanup protection (`System32`, `WinSxS`, `Prefetch`, `Installer`, `Documents`, `Downloads`, `Desktop`, `Pictures`, `Music`, `Videos`, drive root `C:\`)
  - Winget package ID regex validation (`^[a-zA-Z0-9\.\-_+]+$`) preventing command injection
  - Input injection sanitization on power scheme GUIDs and restore point descriptions
  - Dry-run simulation safety
- **54 Unit & Full Automation Feature Tests** (`Win11ControlCenter.UnitTests`):
  - **Full Application Automation Suite (`FullAppAutomationFeatureTests.cs`)**:
    - End-to-end verification of all 15 core feature workflows
    - Full DI Container registration & resolution across all 14 services and 13 ViewModels
    - Automated shell navigation across all 13 pages without dispatcher thread termination
    - Package discovery, Safe Bloat selection, Pin to Top, and Unselect chip tray
    - Software Center category filtering, catalog search, and dry-run execution
    - Startup Manager non-destructive toggling and service manager inventory
    - Privacy baseline application, profile diffing, and cleaner target scanning
    - Custom tweak validation with real-time safety blocks
    - Snapshot session creation and emergency `.reg` generation
    - Activity logging audit trail and settings theme toggling
  - **STA XAML View Instantiation Tests**: Instantiates `MainWindow` and all 13 view templates in an isolated STA thread to prevent XAML parsing and binding regressions.
  - Selection surfacing synchronization and unselect commands in App Debloater & System Cleaner
  - Software Center package discovery, category filtering, and selection
  - Storage clutter scanning and byte formatting
  - Full backup snapshot creation, `.reg` generation, rollback, and deletion
  - Audit logging recording, filtering, and export
  - Custom tweaks validation and storage

### Running the Full Application Automation Suite
A dedicated PowerShell automation runner is included for an immediate live console walkthrough of all features:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-full-automation.ps1
```

Or run all test suites via dotnet CLI:
```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"; $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet test
```

---

## Building and Running

### Prerequisites
- Windows 11 x64 (Build 22000 or later).
- .NET 10 SDK (included or installed via `dotnet-install.ps1`).

### Option A: Run the Standalone Release Executable
A self-contained release package is pre-built in `publish/win-x64/`:
```powershell
.\publish\win-x64\Win11ControlCenter.App.exe
```

### Option B: Build and Run from Source
```powershell
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"; $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet build Win11ControlCenter.slnx
dotnet run --project src/Win11ControlCenter.App
```

### Option C: Install from GitHub Releases
Download either `Win11ControlCenter-Setup-win-x64.exe` (Windows installer) or `Win11ControlCenter-win-x64.zip` (portable) from [GitHub Releases](https://github.com/parasherdhruv/windows-11debloater/releases).

#### SmartScreen Notice & Checksum Verification
Because official releases are built openly in GitHub Actions without a paid Authenticode code-signing certificate, Windows Defender SmartScreen may show *"Windows protected your PC"* on first launch:
1. Click **More info → Run anyway** (or right-click the file → **Properties** → **Unblock**, or run `Unblock-File .\Win11ControlCenter-Setup-win-x64.exe`).
2. Verify the download integrity against the `SHA256SUMS.txt` asset attached to each release:
   ```powershell
   Get-FileHash .\Win11ControlCenter-Setup-win-x64.exe -Algorithm SHA256
   ```
