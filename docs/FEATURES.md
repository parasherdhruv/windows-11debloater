# Win11 Control Center — Features Specification

## 1. Module Overview

**Win11 Control Center** is organized into **13 distinct, fully functional modules** designed for total system visibility, governance, safe debloating, optimization, and software installation. Every module adheres to strict safety boundaries, reversible workflows, and real Windows APIs.

---

## 2. Complete 13-Module Specification

### 1. 📊 Intelligent Dashboard
- **System Telemetry**: Windows edition, OS version, build number, architecture, activation status, CPU model, GPU model, total & available RAM (via `GlobalMemoryStatusEx`), storage distribution (via `DriveInfo`), active power plan, and UAC administrator elevation status.
- **Pending Reboot Detection**: Audits CBS (Component Based Servicing) reboot flags, Windows Update auto-update reboot flags, and pending file rename operations (`PendingFileRenameOperations`).
- **Inventory Metrics**: Live counts for installed user packages, provisioned packages, active startup applications, running services, and backup snapshots.
- **Performance Overview**: Live CPU and RAM utilization metrics queried via genuine Windows performance APIs (`PerformanceCounters` / WMI).
- **Read-Only Scan**: Comprehensive system audit with zero system mutations.

---

### 2. 🗑️ Application Debloater
- **Multi-Source Discovery**: Detects AppX, MSIX, Provisioned packages (`AppxAllUserStore`), Windows Store apps, and traditional Win32 uninstall entries.
- **Clear User vs System Distinction**: Explicit separation between current user profile removal and system-wide provisioning removal.
- **Classification & Risk Levels**: Categorizes apps into:
  - *Safe Consumer Apps*: Xbox, Solitaire, News, Weather, Bing News, Sticky Notes, Clipchamp.
  - *Optional Apps*: Phone Link, Teams Consumer, OneDrive, Paint 3D.
  - *Advanced Candidates*: Voice Recorder, Camera, Windows Maps.
  - *Protected System Infrastructure*: Windows Shell, Start Menu, Defender, Store, VCLibs runtimes.
- **Interactive Quick Shelf / Tray**: A dedicated "Selected for Removal" tray auto-pins selected items to the top, showing real-time selection counts, package IDs, and one-click unselect chips.
- **Segmented Views**: Instant filtering across `All Applications`, `Safe Recommendations`, and `Selected Only`.
- **Protected Package Shields**: Hardcoded safety rules strictly prevent staging or removal of critical Windows components.

---

### 3. 📦 Essential Software Center (Winget)
- **Official Package Manager Integration**: Powered directly by the official Windows Package Manager (`winget.exe`).
- **Curated Open-Source & Essential Catalog**:
  - *Utilities*: 7-Zip, Notepad++, Everything Search, Microsoft PowerToys, ShareX, FastStone Image Viewer.
  - *Browsers*: Mozilla Firefox, Brave Browser, LibreWolf.
  - *Developer Tools*: Visual Studio Code, Git for Windows, Windows Terminal, Node.js LTS, Python 3.
  - *Media & Content*: VLC Media Player, OBS Studio, HandBrake, Sumatra PDF.
  - *Privacy & Notes*: Bitwarden, Obsidian.
- **Batch Selection & Installation**: Select multiple packages across categories and install them sequentially with live progress streaming.
- **Safe Input Sanitization**: Package IDs are validated against strict regex (`^[a-zA-Z0-9\.\-_+]+$`) to prevent CLI argument injection.
- **Dry-Run Simulation**: Simulates the winget execution pipeline without executing downloads or installations.

---

### 4. 🚀 Startup Manager
- **Multi-Source Discovery**: Audits `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`, User Startup folder, and Common Startup folder.
- **Non-Destructive Toggling**: Enables and disables entries using standard Windows `StartupApproved\Run` binary bitmasks (`0x02` for enabled, `0x03` for disabled). Executable files are never deleted or corrupted.
- **Executable Validation**: Inspects executable paths on disk, detects missing or suspicious file references, and flags invalid shortcuts.
- **File Location Reveal**: One-click "Open Location" button opens Windows Explorer with the target binary pre-highlighted.
- **JSON Inventory Export**: Export the full startup audit to `%LOCALAPPDATA%\Win11ControlCenter\Exports\startup_*.json`.

---

### 5. ⚙️ Windows Services Manager
- **Comprehensive SCM Inspection**: Queries the Windows Service Control Manager for Service Name, Display Name, Description, current Status (Running/Stopped), Startup Type (Automatic, Manual, Disabled), binary image path, and service account.
- **Dependency Graph Inspection**: Inspects services depended upon (`ServicesDependedOn`) and dependent services (`DependentServices`) to explain cascading impacts.
- **Categorization**: Windows Core, Networking, Audio, Bluetooth, Printing, Telemetry, Third-Party, and OEM.
- **Hardcoded Protection Shields**: Strict blacklist of 31 core Windows infrastructure services (`RpcSs`, `WinDefend`, `CryptSvc`, `Dhcp`, `Dnscache`, `wuauserv`, `EventLog`, `ProfSvc`, etc.) that cannot be stopped or disabled.
- **JSON Inventory Export**: Export the entire service inventory to `%LOCALAPPDATA%\Win11ControlCenter\Exports\services_*.json`.

---

### 6. 🔒 Privacy & Telemetry Manager
- **10 Core Windows Privacy Settings**:
  1. *Advertising ID*: Disables targeted ad profiling across apps.
  2. *Tailored Experiences*: Prevents Microsoft diagnostic data consumption for personalized tips and ads.
  3. *Diagnostic Data Level*: Minimizes telemetry to the required baseline (`AllowTelemetry = 1`).
  4. *Activity History Cloud Sync*: Disables cloud synchronization of user activities.
  5. *Bing Search in Start*: Disables web suggestions in the Start menu search bar.
  6. *App Launch Tracking*: Stops Windows from tracking launched apps to personalize Start.
  7. *Settings Suggestions*: Disables suggested content in the Windows Settings app.
  8. *Feedback Frequency*: Configures feedback prompts to "Never".
  9. *Location Permissions*: Manages global location tracking flags.
  10. *Search Privacy & History*: Clears and disables device search history logging.
- **Documented Registry Keys**: Uses genuine, documented Windows Group Policy and registry mechanisms.
- **Zero Bogus Blocking**: No giant hosts-file corruptions, broken firewall rules, or DNS resolvers.

---

### 7. ✨ Windows Tweaks & Optimization
- **UI & File Explorer Tweaks**:
  - Show file name extensions (prevents `.exe` spoofing).
  - Show hidden files and folders.
  - Enable compact view in File Explorer.
  - Reduce menu show delay (snappier context menus).
  - Align taskbar icons (Left vs Center).
  - Adjust window minimization/maximization animations.
- **Power Management**:
  - Switch power schemes via `powercfg` (Balanced, High Performance, Power Saver).
- **5 Predefined Optimization Profiles**:
  - *Safe Cleanup*: Minimal non-invasive usability improvements.
  - *Balanced Optimization*: Balanced performance and responsiveness.
  - *Gaming Optimization*: Optimized scheduling and background capture suppression.
  - *Privacy Focused*: Strict telemetry minimization.
  - *Minimal Windows*: Lightweight configuration for constrained hardware.
- **Live Profile State Diff Inspector**: Side-by-side comparison of current system values vs proposed profile values before execution.

---

### 8. 🎮 Gaming Mode Optimization
- **Hardware Audit**: Detects GPU model, active driver version, and display adapter properties.
- **Hardware-Accelerated GPU Scheduling (HAGS)**: Configures GPU hardware scheduling for compatible modern GPUs.
- **Windows Game Mode**: Ensures Windows Game Mode scheduling prioritization is active.
- **Game DVR Background Capture Suppression**: Disables continuous background MP4 buffering and recording, eliminating periodic disk micro-stutters during high-FPS gameplay.
- **Xbox Broadcast Capture Suppression**: Disables unnecessary background game broadcasting hooks.
- **Power Scheme Alignment**: Aligns active power configuration to High Performance profile.
- **No False Claims**: Purely aligns documented Windows scheduling APIs; does not fabricate artificial FPS boosts or bogus RAM purges.

---

### 9. 🧹 System Cleaner & Maintenance
- **Safe Disposable Targets**:
  - User Temporary Files (`%TEMP%`).
  - Windows Temporary Files (`C:\Windows\Temp`).
  - Recycle Bin (via Win32 `SHQueryRecycleBin` and `SHEmptyRecycleBin`).
  - Explorer Thumbnail Cache (`%LOCALAPPDATA%\Microsoft\Windows\Explorer`).
  - Windows Crash Dumps (`%LOCALAPPDATA%\CrashDumps`).
  - Delivery Optimization Cache.
- **2-Hour File Age Grace Period**: Files created or modified within the last 2 hours are skipped to prevent deleting active application scratch files.
- **Strict Blacklist**: Strictly forbids touching `Documents`, `Downloads`, `Desktop`, `Pictures`, `Music`, `Videos`, `System32`, `WinSxS`, `Installer`, or drive root `C:\`.
- **Selected Targets Quick Shelf**: Real-time reclaimable byte calculation, formatted sizing (MB/GB), and instant exclusion chips.

---

### 10. 🛠️ Custom Tweaks Engine
- **JSON-Driven Registry Tweak Builder**: Supports user-defined tweaks targeting `HKCU` or `HKLM`.
- **Supported Value Types**: `DWord` (`REG_DWORD`), `String` (`REG_SZ`), and `QWord` (`REG_QWORD`).
- **Real-Time Safety Validation**: Immediately blocks and rejects tweaks attempting to target dangerous hives or critical system keys:
  - `SAM`
  - `SECURITY`
  - `BCD00000000` (Boot Configuration Data)
  - `CurrentControlSet\Control\SafeBoot`
  - `Session Manager\KnownDLLs`
- **Persistence**: Saved tweaks are securely stored in `%LOCALAPPDATA%\Win11ControlCenter\custom_tweaks.json`.

---

### 11. 🛡️ Restore Center
- **Pre-Optimization Snapshots**: Automatically captures full system snapshots prior to applying configuration changes.
- **Collision-Resistant Timestamps**: Snapshots are isolated in timestamped directories: `%LOCALAPPDATA%\Win11ControlCenter\Backups\{Timestamp}\`.
- **Emergency `.reg` Generator**: Every registry change automatically generates a standalone `pre_apply_rollback.reg` file that can be double-clicked in Windows Explorer for out-of-band recovery without launching the application.
- **Windows System Restore Point Integration**: Integrates directly with Windows System Restore via `Checkpoint-Computer`, with description sanitization to prevent PowerShell injection.
- **One-Click Rollback**: Granular single-session rollback or individual target reversal.

---

### 12. 📜 Activity Logs
- **Persistent JSONL Audit Trail**: Every executed operation is appended to `%LOCALAPPDATA%\Win11ControlCenter\Logs\audit.jsonl`.
- **Structured Fields**: Logs record Operation ID, Timestamp, Category, Action, Target, Pre-Apply Value, Target Value, Status (`Succeeded`, `Failed`, `Skipped`, `RolledBack`), and Error Messages.
- **Severity & Status Filtering**: Real-time filtering by status (All, Success, Failed, RolledBack) and category.
- **Export Capabilities**: Export filtered or full audit records to timestamped JSON files for enterprise record-keeping.

---

### 13. ⚙️ Settings & Appearance
- **Runtime Fluent Theme Switching**: Toggle seamlessly between Fluent Dark Mode (Default) and Fluent Light Mode without restarting the application.
- **Startup Audit Preferences**: Configure automatic system health scan upon application launch.
- **Restore Point Policy**: Configure whether automated Windows System Restore points should be required before multi-step optimizations.
- **Retention Policies**: Configure log retention and backup snapshot cleanup thresholds.
- **Local Settings Storage**: Stored cleanly in `%LOCALAPPDATA%\Win11ControlCenter\settings.json`.
