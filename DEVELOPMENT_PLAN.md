# Win11 Control Center — Phased Development Plan & Acceptance Criteria

This document tracks the phased implementation of **Win11 Control Center**. Each phase must satisfy all defined acceptance criteria and pass automated tests before moving to the next.

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
- [x] Implement Fluent Design WPF Shell (Navigation sidebar with 12 pages, Dark theme, Header bar, Cards).
- [x] Setup structured logging (`ILogger` / JSON logging to local disk).
- [x] Setup Unit Test and Safety Test runners.
- **Acceptance Criteria**:
  - The solution builds cleanly with zero warnings under .NET 10. (VERIFIED)
  - The WPF application compiles and links all 12 navigable tabs. (VERIFIED)
  - Automated tests pass verification in `Win11ControlCenter.UnitTests` and `Win11ControlCenter.SafetyTests`. (VERIFIED - 25 passed, 0 failed).

---

## Phase 2: System Discovery Engine
**Status**: COMPLETED (Passed 32/32 Tests)
- [x] Windows edition, version, build, architecture, and activation status detector.
- [x] Hardware information service (CPU, GPU, RAM, Storage volumes via GlobalMemoryStatusEx, WMI, Registry).
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
- [x] Service manager with Core Windows preservation blacklist.
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
**Status**: NEXT IN QUEUE
- [ ] Supported Privacy Settings engine (Advertising ID, Diagnostic Data, Activity History, Location, App Tracking).
- [ ] UI Optimization settings (Explorer file extensions, hidden files, animations, taskbar).
- [ ] Power management profile engine (Balanced, High Performance, Power Saver).
- [ ] Predefined Optimization Profiles (Safe Cleanup, Balanced, Gaming, Privacy Focused, Minimal).
- **Acceptance Criteria**:
  - Only supported, documented registry paths are modified.
  - Every tweak records pre-change registry values for automated rollback.
  - Profiles present a full diff preview before asking for confirmation.

---

## Phase 6: System Cleaner & Gaming Optimization
**Status**: PENDING
- [ ] Disposable storage auditor (User Temp, Windows Temp, Recycle Bin, Thumbnail Cache).
- [ ] Storage analysis breakdown with exact paths and reclaimable byte counts.
- [ ] Safe cleanup execution handler with strict exclusion blacklist.
- [ ] Gaming optimization module (Windows Game Mode, GPU scheduling, power plan alignment).
- **Acceptance Criteria**:
  - User directories (Documents, Downloads, Desktop) and critical Windows directories (WinSxS, Installer, Prefetch) are never touched.
  - Cleanup accurately reports reclaimed disk space and failed locks.
  - Gaming settings provide transparent explanations without false performance claims.

---

## Phase 7: Recovery & Restore Center
**Status**: PENDING
- [ ] System Restore Point creation and verification via WMI.
- [ ] Backup snapshot explorer and metadata viewer.
- [ ] Reversibility manager: Registry rollback, service state restoration, startup re-enabling.
- [ ] Comprehensive Activity Log viewer with JSON/Text export.
- **Acceptance Criteria**:
  - One-click rollback restores registry values and service startup types to recorded baseline.
  - All operations are indexed with complete audit trail.

---

## Phase 8: Verification, Polish & Packaging
**Status**: PENDING
- [ ] Full automated test suite execution (Unit, Integration, Safety tests).
- [ ] Security audit: least-privilege verification, input sanitization check.
- [ ] UI/UX polish (Fluent styling, animations, empty states, error states).
- [ ] Release build packaging and standalone executable distribution.
- [ ] Complete user and administrator documentation.
- **Acceptance Criteria**:
  - 100% test pass rate across safety and unit test suites.
  - Standalone release package launches cleanly on clean Windows 11 installation.
