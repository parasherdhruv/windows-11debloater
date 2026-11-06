# Win11 Control Center v1.0.0

A modern, modular, and safety-first Windows 11 debloater, optimization dashboard, and system configuration center built in C# and .NET 10 LTS with WPF, MVVM (`CommunityToolkit.Mvvm`), and Dependency Injection.

![Platform](https://img.shields.io/badge/Platform-Windows%2011%20x64-blue)
![Framework](https://img.shields.io/badge/.NET-10.0%20LTS-purple)
![Architecture](https://img.shields.io/badge/Architecture-WPF%20%7C%20MVVM-green)
![Tests](https://img.shields.io/badge/Tests-118%20Passing%20(100%25)-brightgreen)
![License](https://img.shields.io/badge/License-MIT-orange)

---

## Core Philosophy

1. **Safety before performance**: Never disable or remove critical system infrastructure.
2. **Reversibility before convenience**: Every configuration change is backed up before modification with automatic rollback readiness.
3. **Transparency before automation**: Detailed dry-run simulations and diff previews before execution.
4. **User consent before system modifications**: Explicit review dialogs with clear risk tiers.
5. **Measurable optimization instead of fake performance claims**: Genuine Windows APIs, performance counters, and registry keys; zero fake RAM-flush or simulated FPS boosts.
6. **Modular architecture instead of hardcoded scripts**: Clear separation across Core, Infrastructure, PowerShell, SystemEngine, and Presentation layers.

---

## Complete 12-Module Feature Set

1. **📊 Intelligent Dashboard**: Genuine hardware detection (CPU model, GPU model, RAM via `GlobalMemoryStatusEx`, storage via `DriveInfo`, active power scheme, administrator elevation status, and pending reboot flags from CBS and Windows Update). Read-only scan with zero system modifications.
2. **📦 Application Debloater**: Discovers installed AppX, MSIX, Provisioned packages (`AppxAllUserStore`), and traditional Win32 applications. Differentiates user profile installs from provisioned packages. Protected package shields strictly prevent removal of Windows Shell, Store, Defender, or VCLibs dependencies.
3. **🚀 Startup Manager**: Audits Run registry keys (`HKCU`/`HKLM`) and Startup folders. Non-destructive enabling and disabling via `StartupApproved\Run` bitmasks (`0x02` vs `0x03`). Never deletes binary files.
4. **⚙️ Windows Services Manager**: Comprehensive service manager with dependency graph inspection, startup type configuration (Automatic, Manual, Disabled), and service start/stop controls. Hardcoded safety shields protect 28 core Windows infrastructure services (`wuauserv`, `RpcSs`, `WinDefend`, `CryptSvc`, `Dhcp`, `Dnscache`, `AppXSvc`, etc.).
5. **🔒 Privacy & Telemetry**: Manages 10 standard Windows privacy settings (Advertising ID, Tailored Experiences, Diagnostic Data minimization to required baseline, Activity History cloud sync, Bing search in Start, App launch tracking, Settings suggestions) using documented registry mechanisms.
6. **✨ Windows Optimization**: 5 predefined optimization profiles (Safe Cleanup, Balanced Optimization, Gaming Optimization, Privacy Focused, Minimal Windows) + an interactive Live Profile State Diff Inspector. Includes File Explorer usability tweaks (show extensions, show hidden files, compact view, reduce menu show delay) and `powercfg` scheme switching.
7. **🎮 Gaming Optimization**: Hardware-Accelerated GPU Scheduling (HAGS), Windows Game Mode prioritization, Game DVR background MP4 capture buffering elimination (preventing micro-stutters during disk-heavy scenes), Xbox broadcast capture suppression, and High Performance power scheme alignment.
8. **🧹 System Cleaner & Maintenance**: Safely cleans temporary caches (User Temp, Windows Temp, Thumbnail Cache, Crash Dumps, Delivery Optimization) and empties the Recycle Bin using Win32 Shell APIs (`SHQueryRecycleBin` and `SHEmptyRecycleBin`). Spares files modified within the last 2 hours and strictly shields personal folders (`Documents`, `Desktop`, `Downloads`, `Pictures`, `Music`, `Videos`, `System32`, `WinSxS`).
9. **🛠️ Custom Tweaks**: User-defined registry tweaks builder supporting `HKCU` and `HKLM`, subkeys, value names, types (`DWord`, `String`, `QWord`), and target/default values. Real-time safety validation blocks attempts to target critical security or boot hives (`SAM`, `SECURITY`, `BCD00000000`, `SafeBoot`, `KnownDLLs`).
10. **🛡️ Restore Center**: Pre-optimization snapshot repository with collision-resistant timestamps and directory isolation. Generates emergency `pre_apply_rollback.reg` files for offline registry rollback directly through Windows Explorer. Integrates with Windows System Restore (`Checkpoint-Computer`) with description sanitization.
11. **📜 Activity Logs**: Persistent JSONL audit trail (`audit.jsonl`) recording every operation, status (Succeeded, Failed, Skipped, RolledBack), and message. Includes status severity filtering, category filtering, search, and JSON export.
12. **⚙️ Settings & Appearance**: Runtime theme switching between Fluent Dark Mode (Default) and Light Mode. Configuration for startup audits, restore point creation, and audit retention policies stored in `%APPDATA%\Win11ControlCenter\settings.json`.

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

Win11 Control Center includes 118 automated tests passing with a 100% pass rate:

- **85 Safety Tests** (`Win11ControlCenter.SafetyTests`):
  - Protected packages removal prevention (`ShellExperienceHost`, `SecHealthUI`, `StorePurchaseApp`, etc.)
  - Core services preservation (`wuauserv`, `RpcSs`, `WinDefend`, `CryptSvc`, `Dhcp`, etc.)
  - Dangerous registry targets rejection (`SAM`, `SECURITY`, `BCD00000000`, `SafeBoot`, `KnownDLLs`)
  - Directory cleanup protection (`System32`, `WinSxS`, `Prefetch`, `Installer`, `Documents`, `Downloads`, `Desktop`, `Pictures`, `Music`, `Videos`, drive root `C:\`)
  - Input injection sanitization on power scheme GUIDs and restore point descriptions
  - Dry-run simulation safety
- **33 Unit Tests** (`Win11ControlCenter.UnitTests`):
  - Hardware and OS telemetry discovery
  - Reversible startup disabling bitmask calculation
  - Optimization profiles diff calculation and plan compilation
  - Custom profiles JSON serialization and lifecycle
  - Storage clutter scanning and byte formatting
  - Full backup snapshot creation, `.reg` generation, rollback, and deletion
  - Audit logging recording, filtering, and export
  - Custom tweaks validation and storage

Run tests:
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
# Setup environment
$env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"

# Build solution
dotnet build Win11ControlCenter.slnx -c Release

# Run application
dotnet run --project src/Win11ControlCenter.App
```

### Option C: Publish Self-Contained Build
```powershell
dotnet publish src/Win11ControlCenter.App -c Release -r win-x64 --self-contained true -o ./publish/win-x64
```

---

## Project Structure

```
Win11ControlCenter/
├── src/
│   ├── Win11ControlCenter.Core/           # Domain models, enums, interfaces, safety rules
│   ├── Win11ControlCenter.Infrastructure/ # Backup repository, JSONL logging, local settings
│   ├── Win11ControlCenter.PowerShell/     # Typed PowerShell runner with regex validation
│   ├── Win11ControlCenter.SystemEngine/   # System discovery, debloater, tweaks, profiles, cleanup
│   └── Win11ControlCenter.App/            # Modern WPF GUI, MVVM ViewModels, Fluent Dark/Light Theme
│
├── tests/
│   ├── Win11ControlCenter.UnitTests/      # 33 unit tests
│   └── Win11ControlCenter.SafetyTests/    # 85 safety tests
│
├── publish/
│   └── win-x64/Win11ControlCenter.App.exe # Standalone self-contained release executable
│
├── docs/                                  # Architecture, Safety, Features, Rollback docs
├── README.md
└── CONTRIBUTING.md
```

---

## License

This project is licensed under the MIT License.
