# Contributing to Win11 Control Center

Thank you for contributing to **Win11 Control Center**. To maintain system integrity and user trust, all contributions must strictly adhere to the project's engineering principles.

---

## Core Guidelines

1. **Safety First**: Any feature or tweak that destabilizes Windows, breaks Windows Update, disrupts core networking, or disables Windows Defender without consent will be immediately rejected.
2. **Reversibility**: Every new mutation handler must implement a corresponding rollback procedure and record initial state before applying changes.
3. **No Arbitrary Script Execution**: Never evaluate untrusted strings or arbitrary user-supplied PowerShell scripts. All operations must map to typed, allow-listed internal handlers.
4. **No Telemetry / Phoning Home**: The utility is completely local. No network calls to remote analytics or third-party tracking services are permitted.
5. **Architectural Separation**: Keep business and system logic out of ViewModels and WPF code-behind. Use domain services defined in `Win11ControlCenter.Core` and implemented in `Win11ControlCenter.SystemEngine`.

---

## Testing Requirements

- All pull requests modifying system operations or package classifications must include tests in `Win11ControlCenter.SafetyTests`.
- Destructive tests must use mocked providers; never test file deletions or registry mutations on production systems.

