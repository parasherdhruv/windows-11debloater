# Win11 Control Center — Rollback & Recovery Strategy

## 1. Principles of Recovery

A system optimizer is only as safe as its ability to revert changes. Win11 Control Center creates granular, structured rollback artifacts *before* any state modification is applied to the operating system. If an unexpected side effect occurs, the user can restore previous settings either directly within the application or externally using emergency standalone rollback files.

---

## 2. Reversibility Classifications

Every operation item in Win11 Control Center is classified into one of four reversibility tiers:

| Level | Scope / Modules | Rollback Mechanism | User Risk Tier |
|:---|:---|:---|:---:|
| **Fully Reversible** | Registry tweaks, Privacy settings, Optimization profiles, Custom tweaks, Startup entries | Automated atomic rollback via recorded JSON snapshot or one-click `pre_apply_rollback.reg` execution. | **Safe** |
| **Reversible with Backup** | Windows Services (startup type and running state) | Reverts to original startup type (`Automatic`, `Manual`, `Disabled`) and restarts service if it was running. | **Moderate** |
| **Reinstall Required** | Package removals (AppX / MSIX / Provisioned packages) | Re-registers from local `WindowsApps` store or installs via official Winget / Microsoft Store catalog. | **Elevated** |
| **Irreversible** | System Cleaner (Recycle Bin empty, temporary file cache purging) | Cannot be recovered; flagged with explicit confirmation dialog prior to execution. | **Irreversible** |

---

## 3. Structured Backup Repository

All recovery artifacts are stored with collision-resistant timestamps under:
`%LOCALAPPDATA%\Win11ControlCenter\Backups\{Timestamp}\`

```
Backups/
└── 2026-11-18_14-30-00/
    ├── metadata.json              # Session ID, trigger, execution mode, Windows build, elevation
    ├── system-info.json           # Hardware, OS, active power plan snapshot
    ├── pre_apply_rollback.reg     # Standalone executable .reg file for emergency offline restore
    ├── registry/                  # Granular JSON key-value snapshots
    │   └── values.json
    ├── services.json              # Prior startup types and states of affected services
    ├── startup.json               # Startup entries with original registry paths and bitmasks
    ├── applications.json          # Package full names, publisher IDs, and provisioning states
    └── operations.json            # The complete OperationPlan and execution results
```

---

## 4. Recovery Subsystems

### 4.1 Standalone Emergency `.reg` File
Every registry mutation automatically writes a standard Windows registry script (`pre_apply_rollback.reg`) to the backup session directory. 
- **Out-of-band Recovery**: If the application cannot be opened or if Windows Explorer is restarting, the user can navigate to the backup folder and double-click `pre_apply_rollback.reg` to restore the original registry values immediately.
- **Null / Non-Existent Value Handling**: If a registry value did not exist prior to the change, the `.reg` script outputs `"-"` to cleanly delete the created value during restoration.

### 4.2 Windows System Restore Point Integration
- Before applying multi-step optimization profiles or high-impact debloat routines, Win11 Control Center queries whether Windows System Restore is active on the system drive (`C:`).
- If enabled, the application invokes the Windows System Restore API via PowerShell (`Checkpoint-Computer -Description "Win11ControlCenter_PreOptimization_{Timestamp}" -RestorePointType "MODIFY_SETTINGS"`).
- Restore point descriptions are sanitized with strict regex to prevent command injection.

### 4.3 Service Rollback Engine
- Prior to altering any service's startup type or status, the Service Control Manager is queried for:
  - Exact startup type (`Automatic`, `Automatic (Delayed)`, `Manual`, `Disabled`).
  - Running state (`Running`, `Stopped`, `Paused`).
- Rollback re-applies the recorded startup type using `ChangeServiceConfig` and invokes `StartService` if the service was originally running.

### 4.4 Non-Destructive Startup Entry Rollback
- Startup items are **never deleted from disk**.
- Disabling an item modifies the standard Windows binary bitmask in `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run` or `HKLM\...` (`0x02` = Enabled, `0x03` = Disabled).
- Re-enabling simply flips the bitmask back to `0x02`, guaranteeing 100% lossless recovery with zero risk of broken file paths.

### 4.5 Application Package Restoration
- **Current User AppX**: When an AppX package is removed for the current user, its provisioned binary typically remains in `C:\Program Files\WindowsApps`. Win11 Control Center can re-register the package using:
  ```powershell
  Add-AppxPackage -DisableDevelopmentMode -Register "$manifestPath"
  ```
- **Provisioned Packages**: If system-wide provisioning was removed, Win11 Control Center queries the Winget / Microsoft Store catalog to provide an instant re-installation link.

---

## 5. Rollback Verification Workflow

When a rollback is triggered from the **Restore Center**:
1. The snapshot session is loaded and verified for integrity.
2. The pre-change state values are compared against current system values.
3. Handlers execute the inverse operations sequentially.
4. Post-rollback state is queried to verify successful restoration.
5. A `RolledBack` event is recorded in the permanent audit trail (`%LOCALAPPDATA%\Win11ControlCenter\Logs\audit.jsonl`).
