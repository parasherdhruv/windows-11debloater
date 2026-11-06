# Win11 Control Center — Features Specification

## 1. Module Overview

Win11 Control Center is organized into 12 distinct, fully functional modules designed for system visibility, governance, debloating, and optimization.

---

## 2. Modules Detail

### 1. Dashboard
- **System Telemetry**: Windows edition, OS version, build number, CPU model, GPU model, total & available RAM, storage distribution, active power plan, and UAC elevation status.
- **Inventory Metrics**: Counts for installed packages, provisioned packages, active startup applications, running services, and pending reboot state.
- **Performance Overview**: Live CPU and RAM utilization metrics queried via genuine Windows performance APIs (WMI/PerformanceCounters).
- **Read-Only Scan**: Comprehensive system audit with zero mutations.

### 2. Application Debloater
- **Multi-Source Discovery**: Detects AppX, MSIX, Provisioned packages, Store apps, and traditional Win32 uninstall entries.
- **Clear Distinction**: Explicit separation between current user profile removal and system-wide provisioning removal.
- **Classification & Risk Levels**: Categorizes apps into Safe Consumer Apps (Xbox, Solitaire, News, Weather), Optional Apps (Phone Link, Teams Consumer, OneDrive), Advanced Removal Candidates, and Protected System Infrastructure.
- **Multi-Selection & Filter**: Instant search, category filters, batch selection, dry-run simulation, and detailed package inspector.

### 3. Startup Manager
- **Discovery**: Reads from `HKCU\Run`, `HKLM\Run`, Startup Folders (User & Common), and `StartupApproved` keys.
- **Safety First**: Non-destructive disabling. Files are never deleted.
- **Executable Validation**: Verifies binary existence on disk and detects suspicious/missing file references.

### 4. Windows Services Manager
- **Service Inspection**: Name, display name, description, status, current startup type, binary path, service account, and dependency graph.
- **Categorization**: Windows Core, Networking, Audio, Bluetooth, Printing, Telemetry, Third-Party, and OEM.
- **Safety Enforcement**: Hardcoded protection preventing modification of essential Windows services. Advanced Mode required for sensitive services.

### 5. Privacy & Telemetry Manager
- **Supported Windows Settings**: Advertising ID, app launch tracking, tailored experiences, diagnostic data level, activity history, location permissions, feedback frequency, and search privacy.
- **Supported Policies**: Uses genuine Group Policy and documented registry keys. No fake switches or bogus hosts-file domain blocking.

### 6. Windows Optimization
- **UI & Explorer**: Show file extensions, show hidden files, configure taskbar alignment, adjust window animations, disable search web suggestions in Start menu.
- **Power Management**: Select balanced, performance, or power saver profiles with transparent trade-off explanations.
- **Storage Sense**: Configure storage management and safe temporary file policies.

### 7. Gaming Optimization
- **Hardware Audit**: GPU model, active driver, and display adapter info.
- **Safe Gaming Tweakset**: Windows Game Mode toggle, GPU scheduling preferences, gaming power plan alignment, and background application review.
- **No False Claims**: Purely aligns Windows scheduling; does not fabricate artificial FPS boosts.

### 8. System Cleaner
- **Safe Disposable Targets**: User temp folder (`%TEMP%`), Windows temp (`C:\Windows\Temp`), Recycle Bin, and thumbnail caches.
- **Pre-Deletion Audit**: Scans targets, computes reclaimable space, presents file breakdown, and requires explicit user consent.
- **Strict Blacklist**: Never touches User Documents, Downloads, Desktop, browser profiles, WinSxS, Prefetch, or Windows Installer directories.

### 9. Custom Tweaks Engine
- **JSON-Driven Definitions**: Extensible schema defining tweak ID, category, name, risk, scope (`CurrentUser` or `LocalMachine`), registry target, recommended value, and rollback rules.
- **Allow-List Execution**: JSON files cannot inject arbitrary command strings; they map directly to trusted internal handlers.

### 10. Restore Center
- **System Restore Point**: Create and verify restore points.
- **Backup Explorer**: View past optimization sessions, inspect recorded changes, and trigger single-item or full-session rollbacks.
- **Backup Export/Import**: Export configuration snapshots and restore them onto fresh installations.

### 11. Activity Logs
- **Audit Logging**: Every operation is logged with an ID, timestamp, user action, target key/item, pre-change value, post-change value, and outcome.
- **Filtering & Search**: Filter by status (Success, Failed, Rolled Back) and export to JSON or human-readable text.

### 12. Settings
- **Application Preferences**: Dark/Light theme toggle, auto-scan on startup, advanced mode toggle, log retention duration, and backup folder location.

