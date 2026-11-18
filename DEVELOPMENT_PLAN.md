# Win11 Control Center — Phased Development Plan & Acceptance Criteria

This document tracks the phased implementation of **Win11 Control Center**. Each phase defines its implementation deliverables, automated tests, and acceptance criteria.

---

## Progress Overview

| Phase | Description | Status | Tests Passed |
|:---|:---|:---:|:---:|
| **Phase 1** | Foundation & Application Skeleton | ✅ COMPLETED | 25 / 25 |
| **Phase 2** | System Discovery Engine | ✅ COMPLETED | 32 / 32 |
| **Phase 3** | Application Debloater Module | ✅ COMPLETED | 45 / 45 |
| **Phase 4** | Startup & Windows Services Manager | ✅ COMPLETED | 61 / 61 |
| **Phase 5** | Privacy & Windows Optimization | ✅ COMPLETED | 85 / 85 |
| **Phase 6** | System Cleaner & Gaming Optimization | ✅ COMPLETED | 110 / 110 |
| **Phase 7** | Recovery & Restore Center | ✅ COMPLETED | 130 / 130 |
| **Phase 8** | Full App Automation, Polish & Packaging (v1.2.2) | ✅ COMPLETED | 145 / 145 |
| **Phase 9** | Post-v1.2 Enhancements & Future Roadmap | 🔄 PLANNED | — |

---

## Phase 1: Foundation & Application Skeleton
**Status**: COMPLETED (Passed 25/25 Tests)
- [x] Create .NET 10 Solution (`Win11ControlCenter.slnx`) with modular project separation:
  - `Win11ControlCenter.Core`
  - `Win11ControlCenter.Infrastructure`
  - `Win11ControlCenter.SystemEngine`
  - `Win11ControlCenter.PowerShell`
  - `Win11ControlCenter.App` (WPF)
  - `Win11ControlCenter.UnitTests`
  - `Win11ControlCenter.SafetyTests`
- [x] Configure Dependency Injection (`Microsoft.Extensions.DependencyInjection`).
- [x] Implement MVVM infrastructure (`CommunityToolkit.Mvvm`).
- [x] Implement Fluent Design WPF Shell (Navigation sidebar with 13 pages, Dark/Light theme, Header bar, Cards).
- [x] Setup structured logging (`ILogger` / JSON logging to local disk).
- [x] Setup Unit Test and Safety Test runners.
- **Acceptance Criteria**:
  - The solution builds cleanly with zero warnings under .NET 10. (VERIFIED)
  - The WPF application compiles and links all navigable tabs. (VERIFIED)
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 25 passed, 0 failed).

---

## Phase 2: System Discovery Engine
**Status**: COMPLETED (Passed 32/32 Tests)
- [x] Windows edition, version, build, architecture, and activation status detector.
- [x] Hardware information service (CPU, GPU, RAM via `GlobalMemoryStatusEx`, Storage volumes via `DriveInfo`, WMI, Registry).
- [x] Installed AppX/MSIX and Provisioned Package scanner.
- [x] Win32 installed software scanner (Registry uninstall keys).
- [x] Windows Services scanner (Name, DisplayName, StartupType, Status, BinaryPath, Core Services tagging).
- [x] Startup items scanner (`Run`, `RunOnce`, Startup directories, `StartupApproved` bitmasks).
- [x] Live resource metrics reader (CPU % and Available RAM).
- **Acceptance Criteria**:
  - Genuine system metrics are retrieved via read-only Windows APIs and CIM/Registry. (VERIFIED - detects Windows 11 Build 26300, Ryzen 5 9600X, RTX 5070, 16GB RAM, pending reboot).
  - The Dashboard populates with actual hardware, OS, and inventory figures. (VERIFIED)
  - Read-only scan executes asynchronously without freezing the UI thread. (VERIFIED)
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 32 passed, 0 failed).

---

## Phase 3: Application Debloater Module
**Status**: COMPLETED (Passed 45/45 Tests)
- [x] Domain catalog with risk levels: Safe Consumer, Optional, Advanced, and Protected.
- [x] Protected packages filtering engine (hard protection prevents selection/removal).
- [x] Current-User AppX removal handler vs System-Wide Provisioning removal handler (`PackageRemovalOperationHandler`).
- [x] Multi-select application UI with search, category filtering, package inspection card, and operation summary preview dialog.
- [x] Interactive "Selected for Removal" Quick Shelf / Tray with chip indicators and instant unselect commands.
- [x] 10-step Execution Engine integration: Planning, Dry-Run preview, Backup metadata, and Execution.
- [x] Input sanitization and command injection prevention verified.
- **Acceptance Criteria**:
  - Protected packages (Shell, Store, Accounts, Security) can never be staged for removal. (VERIFIED - 6 safety test theories passed).
  - Malicious inputs and injection attempts are rejected. (VERIFIED - 5 command injection theories passed).
  - Dry-run mode accurately previews package removal without executing PowerShell/DISM mutations. (VERIFIED).
  - Removal verifies postcondition state and logs package identity. (VERIFIED).
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 45 passed, 0 failed).

---

## Phase 4: Startup & Windows Services Manager
**Status**: COMPLETED (Passed 61/61 Tests)
- [x] Startup entry disable/enable engine (non-destructive `StartupApproved` bitmasks).
- [x] Startup management UI with status badges, search filter, and executable path inspector.
- [x] File location reveal in File Explorer (`OpenFileLocation`).
- [x] Service manager with Core Windows preservation blacklist (31 protected services).
- [x] Service dependency viewer (`sc.ServicesDependedOn`, `sc.DependentServices`) and risk explanation.
- [x] Safe startup type configuration (Automatic, Manual, Disabled).
- [x] JSON inventory export for both startup applications and Windows services.
- **Acceptance Criteria**:
  - Core services (`RpcSs`, `WinDefend`, `EventLog`, `Dnscache`, `wuauserv`, `MpsSvc`, etc.) cannot be modified or stopped. (VERIFIED - 13 theories passed).
  - Startup disabling is completely reversible without deleting files. (VERIFIED - uses `StartupApproved\Run` bitmasks).
  - Original service startup types are stored in the backup record before modification. (VERIFIED).
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 61 passed, 0 failed).

---

## Phase 5: Privacy & Windows Optimization
**Status**: COMPLETED (Passed 85/85 Tests)
- [x] Supported Privacy Settings engine (Advertising ID, Diagnostic Data minimization, Activity History, App Tracking, Bing Search in Start, Tailored Experiences).
- [x] Documented registry-based modifications without hosts-file corruptions or fake domain blockers.
- [x] UI Optimization settings (Explorer file extensions, hidden files, compact view, reduce menu show delay).
- [x] Power management profile engine (Balanced, High Performance, Power Saver via `powercfg`).
- [x] Predefined Optimization Profiles (Safe Cleanup, Balanced Optimization, Gaming Optimization, Privacy Focused, Minimal Windows).
- [x] Live Profile State Diff Inspector displaying pre-apply and target values side by side.
- **Acceptance Criteria**:
  - Only supported, documented registry paths are modified. (VERIFIED).
  - Every tweak records pre-change registry values for automated rollback. (VERIFIED).
  - Profiles present a full diff preview before asking for confirmation. (VERIFIED).
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 85 passed, 0 failed).

---

## Phase 6: System Cleaner & Gaming Optimization
**Status**: COMPLETED (Passed 110/110 Tests)
- [x] Safe disposable storage auditor (User Temp, Windows Temp, Recycle Bin, Thumbnail Cache, Crash Dumps, Delivery Optimization).
- [x] Storage analysis breakdown with exact paths and reclaimable byte counts.
- [x] 2-hour file age grace period preventing deletion of active application scratch files.
- [x] Safe cleanup execution handler with strict exclusion blacklist protecting user personal folders (`Documents`, `Desktop`, `Downloads`, `Pictures`, `Music`, `Videos`, `System32`, `WinSxS`).
- [x] Selected Targets Quick Shelf with real-time size calculations.
- [x] Shell API integration (`SHQueryRecycleBin`, `SHEmptyRecycleBin`).
- [x] Gaming optimization module: Hardware-Accelerated GPU Scheduling (HAGS), Windows Game Mode prioritization, Game DVR background MP4 buffering elimination, Xbox broadcast suppression, and High Performance power scheme.
- **Acceptance Criteria**:
  - User directories and critical Windows system directories are strictly protected. (VERIFIED).
  - Cleanup accurately reports reclaimed disk space and failed locks. (VERIFIED).
  - Gaming settings provide transparent explanations without false performance claims. (VERIFIED).
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 110 passed, 0 failed).

---

## Phase 7: Recovery & Restore Center
**Status**: COMPLETED (Passed 130/130 Tests)
- [x] System Restore Point creation and verification via `Checkpoint-Computer` with sanitized descriptions.
- [x] Pre-optimization snapshot repository with collision-resistant timestamps in `%LOCALAPPDATA%\Win11ControlCenter\Backups\`.
- [x] Emergency `pre_apply_rollback.reg` generator allowing out-of-band registry rollback directly via Windows Explorer.
- [x] Reversibility manager: Registry rollback, service state restoration, startup re-enabling.
- [x] Comprehensive Activity Log viewer with JSONL persistent audit trail (`audit.jsonl`), status filters, and export.
- [x] Custom Tweaks Engine: JSON-driven custom registry tweak builder (`HKCU`/`HKLM`, `DWord`, `String`, `QWord`) with real-time safety validation blocking dangerous keys (`SAM`, `SECURITY`, `BCD00000000`, `SafeBoot`, `KnownDLLs`).
- **Acceptance Criteria**:
  - One-click rollback restores registry values and service startup types to recorded baseline. (VERIFIED).
  - All operations are indexed with complete audit trail. (VERIFIED).
  - Malicious registry targets in custom tweaks are blocked in real-time. (VERIFIED).
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 130 passed, 0 failed).

---

## Phase 8: Full App Automation, Polish & Packaging (v1.2.2)
**Status**: COMPLETED (Passed 145/145 Tests — 100% Pass Rate)
- [x] **Essential Software Center (Winget)**: Curated software catalog, category filtering, search, batch install, live progress streaming, and dry-run execution.
- [x] **Full Application Automation Suite (`FullAppAutomationFeatureTests.cs`)**:
  - Verified 15 core feature workflows end-to-end.
  - Complete DI container registration and resolution (13 services + 13 ViewModels).
  - Automated shell navigation across all 13 pages without dispatcher thread termination.
- [x] **STA XAML View Instantiation Suite**: Verified `MainWindow` and all 13 view templates in isolated STA threads.
- [x] **Runtime Theme Switching**: Fluent Dark Mode (Default) and Light Mode via `ThemeService`.
- [x] **Standalone Release Build & Packaging**:
  - Self-contained Windows x64 binary in `publish/win-x64/Win11ControlCenter.App.exe`.
  - Inno Setup installer script (`installer/Win11ControlCenter.iss`) producing `Win11ControlCenter-Setup-win-x64.exe`.
  - GitHub Actions CI/CD release workflow (`.github/workflows/release.yml`) with automated SHA-256 checksum generation.
- [x] **Safety Verification**:
  - 91 safety tests in `Win11ControlCenter.SafetyTests`.
  - 54 unit & automation tests in `Win11ControlCenter.UnitTests`.
  - Dedicated PowerShell test runners: `run-automation-tests.ps1` and `scripts\run-full-automation.ps1`.
- **Acceptance Criteria**:
  - 100% test pass rate across safety and unit test suites (145/145 passed). (VERIFIED).
  - Standalone release package launches cleanly on clean Windows 11 installation. (VERIFIED).
  - Official v1.2.2 release artifacts tagged and published. (VERIFIED).

---

## Phase 9: Post-v1.2 Enhancements & Future Roadmap
**Status**: PLANNED
- [ ] **Multi-Language Localization (i18n)**: XAML resource dictionary localization for Spanish, German, French, Japanese, and Simplified Chinese.
- [ ] **Community Tweak Presets Marketplace/Importer**: Safe, validated schema for importing curated community optimization profiles with cryptographic signature checks.
- [ ] **Headless / CLI Mode**: Command-line interface (`Win11ControlCenter.Cli.exe`) for automated enterprise deployment and scripting with dry-run support.
- [ ] **Hardware Sensor Monitoring**: Optional lightweight CPU temperature and GPU clock telemetry via LibreHardwareMonitor integration.
- [ ] **Scheduled Automated Maintenance**: Windows Task Scheduler integration for non-disruptive weekly cleanup and restore point rotation.
- [ ] **Enterprise GPO Export**: Export configured privacy and optimization profiles directly into Group Policy template (`.admx`/`.adml`) files.
