# Win11 Control Center — Safety Specification & Governance

## 1. Safety Philosophy

System stability, data security, and user transparency are foundational to Win11 Control Center. We reject the "scorched-earth" approach common in public debloater scripts, which break core Windows subsystems, prevent future Windows updates, corrupt user profiles, or trigger blue-screen errors (BSODs).

---

## 2. Protected Applications (Non-Removable)

The following categories and packages are classified as **Protected** and are blocked from removal in the planning layer:

### Critical Shell & System Infrastructure
- `Microsoft.Windows.ShellExperienceHost`
- `Microsoft.Windows.StartMenuExperienceHost`
- `Microsoft.Windows.Search`
- `Microsoft.Windows.Explorer`
- `Microsoft.Windows.SecHealthUI` (Windows Security / Defender)
- `Microsoft.WindowsStore` (Store infrastructure is preserved by default to allow re-installation of consumer apps)
- `Microsoft.DesktopAppInstaller` (Winget installer engine)
- `Microsoft.StorePurchaseApp`
- `Microsoft.XboxIdentityProvider` (Critical for many Microsoft authentication workflows, even non-gaming)
- `Microsoft.AAD.BrokerPlugin` (Entra / Microsoft Account authentication)
- `Microsoft.AccountsControl` (Account settings and login integration)

### Frameworks & Runtimes
- `Microsoft.VCLibs.*` (Visual C++ Universal Runtime dependencies)
- `Microsoft.NET.Native.*` (.NET Native Runtimes)
- `Microsoft.UI.Xaml.*` (WinUI 2.x and 3.x modern controls)
- `Microsoft.DirectX.*`

### System Capacities & Capabilities
- System Notepad & Calculator are categorized as "Optional / Safe", but replacement warnings are given before removal.
- Edge WebView2 Runtime (`Microsoft.EdgeWebView2`) is strictly protected as countless desktop applications rely on it.

*Policy: Any package not explicitly listed in the verified catalog defaults to `Unclassified / Protected` until reviewed.*

---

## 3. Core Services Protection

The Windows Services Manager enforces strict safeguards against modifying services essential for boot, networking, hardware management, and security:

### Absolute Blacklist (Cannot be Stopped or Disabled)
| Service Name | Display Name | Reason for Protection |
| :--- | :--- | :--- |
| `RpcSs` | Remote Procedure Call (RPC) | Foundational system IPC; disabling renders Windows unbootable |
| `RpcEptMapper` | RPC Endpoint Mapper | Mandatory for RPC communication |
| `DcomLaunch` | DCOM Server Process Launcher | Core COM/DCOM infrastructure |
| `EventLog` | Windows Event Log | Mandatory for auditing, diagnosis, and service startup |
| `CryptSvc` | Cryptographic Services | Windows Update, certificate validation, driver signatures |
| `WinDefend` | Microsoft Defender Antivirus Service | Security baseline protection |
| `SecurityHealthService` | Windows Security Service | Security status reporting |
| `MpsSvc` | Windows Defender Firewall | Host network security |
| `Dhcp` | DHCP Client | Network address acquisition |
| `Dnscache` | DNS Client | Domain resolution |
| `LanmanWorkstation` | Workstation | Network I/O and file shares |
| `Schedule` | Task Scheduler | Critical Windows maintenance & background tasks |
| `PlugPlay` | Plug and Play | Device hardware detection and configuration |
| `Power` | Power | System power state management |
| `AudioEndpointBuilder` | Windows Audio Endpoint Builder | Audio hardware detection |
| `AudioSrv` | Windows Audio | Audio playback |
| `ProfSvc` | User Profile Service | User account logon |
| `SamSs` | Security Accounts Manager | User authentication |
| `LSM` | Local Session Manager | Interactive session handling |
| `SystemEventsBroker` | System Events Broker | Modern background execution |
| `TimeBrokerSvc` | Time Broker | Modern background task coordination |

### Conservative Modification Rules:
- Services are never deleted (`sc.exe delete` is prohibited).
- Services are set to `Manual (Demand Start)` rather than `Disabled` whenever possible, preserving the ability of Windows to launch them on demand.
- Bulk service disabling is disallowed.

---

## 4. Forbidden Optimizations (Out-of-Scope Anti-Patterns)

Win11 Control Center will **NEVER** implement or offer the following destructive tweaks:
1. **Disabling Windows Update (`wuauserv`)**: Leaving systems unpatched invites remote code execution vulnerabilities.
2. **Disabling Windows Defender / SmartScreen**: Security degradation is not performance optimization.
3. **Disabling Memory Compression or Pagefile**: Induces out-of-memory crashes and instability under load.
4. **Disabling Core Isolation / VBS / Secure Boot / TPM**: Compromises hardware-enforced integrity.
5. **Editing the Windows `hosts` file with giant blocklists**: Degrades DNS resolution performance and triggers networking bugs.
6. **Purging WinSxS / Component Store files manually**: Causes servicing corruption and future update failures.
7. **Modifying undocumented NT kernel flags**: Risk of subtle data corruption.

---

## 5. Security & Elevation Architecture

1. **Least Privilege by Default**:
   - The application starts with standard user rights.
   - Read-only operations (auditing, scanning, querying packages, viewing services) execute without UAC elevation.
2. **Just-In-Time Elevation**:
   - Only when applying plans containing machine-wide (`HKLM`, Service Control Manager, DISM) operations is elevation requested.
3. **Immutable Operation Handlers**:
   - The execution engine only invokes hardcoded, strongly typed handlers (`RegistryOperationHandler`, `ServiceOperationHandler`, `AppXOperationHandler`, `ScheduledTaskOperationHandler`).
   - No dynamic PowerShell string evaluation (`Invoke-Expression` or `ScriptBlock.Create`) with user-supplied arguments is permitted.
4. **Enterprise Awareness**:
   - If the system is domain-joined or enrolled in MDM (Intune), settings managed by Group Policy or MDM are tagged as **Managed (Read-Only)** to prevent policy conflicts.

