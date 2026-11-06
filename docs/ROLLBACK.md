# Win11 Control Center — Rollback & Recovery Strategy

## 1. Principles of Recovery

A system optimizer is only as safe as its ability to revert changes. Win11 Control Center creates granular, structured rollback artifacts *before* any state modification is applied.

---

## 2. Reversibility Classifications

Every operation item explicitly declares its reversibility tier:

| Level | Description | Rollback Mechanism |
| :--- | :--- | :--- |
| **Fully Reversible** | Changes to registry values, startup items, or services where previous values were recorded. | Automated atomic rollback via recorded snapshot. |
| **Reversible with Backup** | Complex configuration changes where restoring original state requires reloading saved config files. | Automated import from backup folder. |
| **Reinstall Required** | Package removals (AppX/MSIX/Provisioned packages). | Automated reinstall via Microsoft Store or winget catalog where available. |
| **Irreversible** | Permanent file deletions (e.g., emptying Recycle Bin, temp file cleanup). | Cannot be restored; clearly flagged with irreversible warning prior to execution. |

---

## 3. Structured Backup Repository

Backups are placed in `%LocalAppData%\Win11ControlCenter\Backups\{Timestamp}\`:

```
Backups/
└── 2026-11-06_14-30-00/
    ├── metadata.json       # Session info, trigger, user status, Windows build
    ├── system-info.json    # Hardware, OS, power plan snapshot
    ├── registry/           # Standard .reg files and JSON key-value snapshots
    │   ├── pre_apply.reg
    │   └── values.json
    ├── services.json       # Startup type and state of affected services
    ├── startup.json        # Startup entries with original registry paths/values
    ├── applications.json   # Package full names, publisher IDs, provisioning state
    └── operations.json     # The complete OperationPlan and execution results
```

---

## 4. Recovery Subsystems

### 4.1 System Restore Point Integration
- Before applying high-impact or multi-step profiles, Win11 Control Center checks whether System Restore is enabled on the system drive (`C:`).
- If enabled, the application invokes the Windows System Restore API / WMI (`Checkpoint-Computer` / `SRSetRestorePointW`) to create a dedicated restore point: `Win11ControlCenter_PreOptimization_{Timestamp}`.

### 4.2 Registry Rollback
- Original values (including type `REG_SZ`, `REG_DWORD`, `REG_QWORD`, `REG_BINARY`, etc.) are captured.
- If a key or value did not previously exist, the rollback records an explicit `DeleteValue` or `DeleteKey` directive.
- Generates both an executable `.reg` file (for manual emergency recovery outside the app) and a structured JSON definition for in-app restoration.

### 4.3 Service Rollback
- Captures the exact startup mode (`Automatic`, `Automatic (Delayed)`, `Manual`, `Disabled`) prior to change.
- Rollback restores the recorded mode and triggers a service restart if the service was originally running.

### 4.4 Startup Item Rollback
- Startup entries are **never deleted**.
- Disabling a startup entry moves or masks the item using standard Windows `StartupApproved` registry keys or appends a `.disabled` sentinel, making re-enabling an instant, lossless operation.

### 4.5 Application Restoration
- For removed AppX packages:
  - If the package is still provisioned in `C:\Program Files\WindowsApps`, it can be re-registered for the current user via `Add-AppxPackage -DisableDevelopmentMode -Register ...`.
  - If the provisioned package was also removed, the app queries the winget or Microsoft Store catalog to provide a one-click re-installation link.

