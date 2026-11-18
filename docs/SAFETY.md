# Win11 Control Center — Safety Specification & Governance

## 1. Safety Philosophy

System stability, platform security, and user transparency are foundational to Win11 Control Center. We firmly reject the "scorched-earth" approach common in public debloater scripts, which break core Windows subsystems, prevent future Windows updates, corrupt user profiles, or trigger blue-screen errors (BSODs).

All system interactions in Win11 Control Center are governed by hardcoded, compile-time safety shields verified by **91 dedicated safety tests** in `Win11ControlCenter.SafetyTests`.

---

## 2. Protected Applications (Non-Removable)

The following packages are classified as **Protected** in `Win11ControlCenter.Core.Rules.ProtectedPackages`. They are completely shielded from removal, staging, or dry-run execution:

### Exact Package Identifiers (26 Core Components)
1. `Microsoft.Windows.ShellExperienceHost` — Core Windows shell rendering
2. `Microsoft.Windows.StartMenuExperienceHost` — Windows 11 Start Menu process
3. `Microsoft.Windows.Search` — Windows search indexer & search interface
4. `Microsoft.Windows.SecHealthUI` — Windows Security / Defender user interface
5. `Microsoft.WindowsStore` — Microsoft Store infrastructure (needed for app restoration)
6. `Microsoft.DesktopAppInstaller` — Official Windows Package Manager (`winget.exe`) engine
7. `Microsoft.StorePurchaseApp` — Store purchasing and license validation broker
8. `Microsoft.XboxIdentityProvider` — Essential for Microsoft account auth tokens (even non-gaming)
9. `Microsoft.AAD.BrokerPlugin` — Microsoft Entra ID / Work & School account broker
10. `Microsoft.AccountsControl` — Windows Settings account management broker
11. `Microsoft.BioEnrollment` — Windows Hello biometric authentication host
12. `Microsoft.CredDialogHost` — Windows credential prompt dialog
13. `Microsoft.ECApp` — Eye control & accessibility runtime
14. `Microsoft.LockApp` — Windows Lock Screen host
15. `Microsoft.Win32WebViewHost` — Legacy Win32 WebView host
16. `Microsoft.EdgeWebView2` — Microsoft Edge WebView2 runtime (relied on by countless modern apps)
17. `Microsoft.Windows.CloudExperienceHost` — Out-of-Box Experience (OOBE) and account setup
18. `Microsoft.Windows.Apprep.ChxApp` — SmartScreen application reputation dialog
19. `Microsoft.Windows.ContentDeliveryManager` — Windows feature and layout delivery
20. `Microsoft.Windows.OOBENetworkCaptivePortal` — Network captive portal authentication
21. `Microsoft.Windows.OOBENetworkConnectionFlow` — Network setup wizard during onboarding
22. `Microsoft.Windows.ParentalControls` — Microsoft Family Safety controls
23. `Microsoft.Windows.PeopleExperienceHost` — Shell contact card infrastructure
24. `Microsoft.Windows.PinningConfirmationDialog` — Taskbar pinning security confirmation
25. `Microsoft.Windows.SecureAssessmentBrowser` — Take a Test / secure lockdown browser
26. `Microsoft.Windows.XGpuEjectDialog` — External GPU safe disconnection dialog

### Protected Runtime Prefixes
Any package matching the following runtime prefixes is strictly protected:
- `Microsoft.VCLibs*` — Visual C++ Universal Runtime dependencies
- `Microsoft.NET.Native*` — .NET Native Framework and runtime packages
- `Microsoft.UI.Xaml*` — WinUI modern XAML control libraries
- `Microsoft.DirectX*` — DirectX runtime libraries
- `Microsoft.Services.Store.Engagement*` — Store engagement telemetry libraries
- `Microsoft.Advertising.Xaml*` — Ad platform runtime dependencies

---

## 3. Core Services Protection (31 Blacklisted Services)

The Windows Services Manager in `Win11ControlCenter.Core.Rules.CoreWindowsServices` enforces an absolute blacklist preventing the stopping, disabling, or reconfiguration of 31 vital Windows services:

| Service Name | Display Name | Reason for Absolute Blacklist |
|:---|:---|:---|
| `RpcSs` | Remote Procedure Call (RPC) | Foundational IPC; disabling renders Windows unbootable |
| `RpcEptMapper` | RPC Endpoint Mapper | Mandatory for all RPC communications |
| `DcomLaunch` | DCOM Server Process Launcher | Core COM/DCOM infrastructure |
| `EventLog` | Windows Event Log | Mandatory for system diagnosis, auditing, and service startup |
| `CryptSvc` | Cryptographic Services | Windows Update, driver verification, certificate management |
| `WinDefend` | Microsoft Defender Antivirus | Endpoint security baseline protection |
| `SecurityHealthService` | Windows Security Service | Security status aggregation and reporting |
| `MpsSvc` | Windows Defender Firewall | Host network firewall security |
| `Dhcp` | DHCP Client | IP address acquisition and lease renewal |
| `Dnscache` | DNS Client | Domain name resolution and caching |
| `LanmanWorkstation` | Workstation | Network I/O and SMB file share client |
| `LanmanServer` | Server | File, printer, and named pipe sharing |
| `Schedule` | Task Scheduler | Windows maintenance tasks, updates, and background jobs |
| `PlugPlay` | Plug and Play | Device hardware detection, enumeration, and driver binding |
| `Power` | Power | System power state, sleep, and thermal management |
| `AudioEndpointBuilder` | Windows Audio Endpoint Builder | Audio hardware detection and endpoint enumeration |
| `AudioSrv` | Windows Audio | System audio playback and routing |
| `ProfSvc` | User Profile Service | User account logon, session setup, and profile loading |
| `SamSs` | Security Accounts Manager | User authentication and local credential verification |
| `LSM` | Local Session Manager | Interactive session and logon handling |
| `SystemEventsBroker` | System Events Broker | Coordinates modern background app events |
| `TimeBrokerSvc` | Time Broker | Modern background task timer coordination |
| `BrokerInfrastructure` | Background Infrastructure Broker | Windows background task infrastructure broker |
| `CoreMessagingRegistrar` | CoreMessaging | Modern UI message routing and dispatching |
| `KeyIso` | CNG Key Isolation | LSA key isolation and cryptography security |
| `VaultSvc` | Credential Manager | Secure credential and token storage |
| `StateRepository` | State Repository Service | AppX package metadata and application state repository |
| `AppXSvc` | AppX Deployment Service | Deploying, registering, and maintaining modern apps |
| `ClipSVC` | Client License Service | Windows Store license validation |
| `wuauserv` | Windows Update | Mandatory security patch delivery and servicing |
| `TrustedInstaller` | Windows Modules Installer | Installing, modifying, and removing Windows components |

---

## 4. Storage Cleaner Safety Blacklist

The System Cleaner enforces strict directory exclusions in `SafetyValidator.ValidateOperation`:

### Drive Root Exclusion
- Cleaning drive roots (e.g., `C:\`, `D:\`) is strictly rejected.

### Protected System Directories
- `Windows\System32`
- `Windows\WinSxS`
- `Windows\Installer`
- `Windows\Prefetch`
- `Program Files`
- `Program Files (x86)`

### Protected Personal User Folders
- `Documents`
- `Downloads`
- `Desktop`
- `Pictures`
- `Music`
- `Videos`

### 2-Hour File Age Grace Period
Temporary files created or modified within the past 2 hours are automatically skipped, ensuring that active software installations, browser downloads, or compiler caches are not prematurely purged.

---

## 5. Registry Safety Blacklist

Custom tweaks and profile changes are evaluated against protected hives in `SafetyValidator`:
- `SAM` — Security Accounts Manager hive
- `SECURITY` — Windows security policies and LSA secrets hive
- `BCD00000000` — Boot Configuration Data
- `CurrentControlSet\Control\SafeBoot` — Safe Mode boot configurations
- `Session Manager\KnownDLLs` — Core operating system binary loading paths

Any operation targeting these keys is immediately rejected with a safety validation error.

---

## 6. Input Injection Sanitization

All dynamic parameters passed to external utilities or PowerShell cmdlets are sanitized:
- **Winget Package IDs**: Validated against `^[a-zA-Z0-9\.\-_+]+$` to prevent argument injection.
- **Power Scheme GUIDs**: Validated against standard GUID format (`^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$`).
- **Restore Point Descriptions**: Sanitized to remove quotes, dollar signs, semicolons, and pipeline characters before passing to `Checkpoint-Computer`.

---

## 7. Forbidden Anti-Patterns (Out of Scope)

Win11 Control Center will **NEVER** implement the following destructive tweaks:
1. **Disabling Windows Update (`wuauserv`)**: Exposes systems to unpatched remote code execution vulnerabilities.
2. **Disabling Windows Defender / SmartScreen**: Security degradation is never optimization.
3. **Disabling Memory Compression or Pagefile**: Causes out-of-memory crashes under heavy loads.
4. **Disabling Core Isolation / VBS / Secure Boot / TPM**: Compromises hardware-enforced boot and kernel security.
5. **Editing Hosts File with Giant Blocklists**: Degrades DNS resolution performance and introduces networking latency.
6. **Purging WinSxS / Component Store manually**: Corrupts servicing state and breaks cumulative updates.
7. **Modifying Undocumented NT Kernel Flags**: Risk of silent file system and memory corruption.
